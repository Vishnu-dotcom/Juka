﻿using JukaCompiler.Exceptions;
using JukaCompiler.Extensions;
using JukaCompiler.Lexer;
using JukaCompiler.Parse;
using JukaCompiler.Statements;
using JukaCompiler.SystemCalls;
using Microsoft.Extensions.DependencyInjection;

namespace JukaCompiler.Interpreter
{
    internal class JukaInterpreter : Stmt.Visitor<Stmt>, Expression.Visitor<object>
    {
        private ServiceProvider serviceProvider;
        private JukaEnvironment globals;
        private JukaEnvironment environment;
        private Dictionary<Expression, int?> locals = new Dictionary<Expression, int?>();

        internal JukaInterpreter(ServiceProvider services)
        {
            environment = globals = new JukaEnvironment();
            this.serviceProvider = services;

            if (serviceProvider != null)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                globals.Define("clock", serviceProvider.GetService<ISystemClock>());
                globals.Define("fileOpen", services.GetService<IFileOpen>());
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else
            {
                throw new JRuntimeException("Unable to load system calls");
            }
        }

        internal void Interpert(List<Stmt> statements)
        {
            foreach(var stmt in statements)
            {
                Execute(stmt);
            }
        }
        private void Execute(Stmt stmt)
        {
            stmt.Accept(this);
        }

        internal void ExecuteBlock(List<Stmt> statements, JukaEnvironment environment)
        {
            JukaEnvironment previous = this.environment;

            try
            {
                this.environment = environment;
                foreach(Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                this.environment = previous;
            }
        }

        Stmt Stmt.Visitor<Stmt>.VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.statements, new JukaEnvironment(this.environment));
            return null;
        }

        Stmt Stmt.Visitor<Stmt>.VisitFunctionStmt(Stmt.Function stmt)
        {
            JukaFunction functionCallable = new JukaFunction(stmt, null, false);
            environment.Define(stmt.name.ToString(), functionCallable);
            return null;
        }

        public Stmt VisitClassStmt(Stmt.Class stmt)
        {
            throw new NotImplementedException();
        }

        public Stmt VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.expression);
            return null;
        }

        public Stmt VisitIfStmt(Stmt.If stmt)
        {
            Stmt.DefaultStatement defaultStatement = new Stmt.DefaultStatement();

            if (IsTrue(Evaluate(stmt.condition)))
            {
                Execute(stmt.thenBranch);
            }
            else
            { 
                Execute(stmt.elseBranch);
            }

            return defaultStatement;
        }

        public Stmt VisitPrintLine(Stmt.PrintLine stmt)
        {
            if (stmt.expr != null)
            {
                if (stmt.expr is Expression.Literal || stmt.expr is Expression.LexemeTypeLiteral)
                { 
                    var lexemeTypeLiteral = Evaluate(stmt.expr) as Expression.LexemeTypeLiteral;
                    Console.WriteLine(lexemeTypeLiteral.Literal);
                    return new Stmt.PrintLine();
                }

                if (stmt.expr is Expression.Variable)
                {
                    var variable = LookUpVariable(stmt.expr.Name, stmt.expr);
                    if (variable != null)
                    {
                        if (variable is Expression.LexemeTypeLiteral)
                        {
                            Console.WriteLine(((Expression.LexemeTypeLiteral)variable).Literal);
                        }
                    }
                }
            }

            return new Stmt.PrintLine();
        }

        public Stmt VisitPrint(Stmt.Print stmt)
        {
            if (stmt.expr != null)
            {
                if (stmt.expr is Expression.LexemeTypeLiteral)
                {
                    var lexemeTypeLiteral = Evaluate(stmt.expr) as Expression.LexemeTypeLiteral;
                    Console.Write(lexemeTypeLiteral.Literal);
                    return new Stmt.Print();
                }

                if (stmt.expr is Expression.Variable)
                {
                    var variable = LookUpVariable(stmt.expr.Name, stmt.expr);
                    if (variable != null)
                    {
                        if (variable is Expression.LexemeTypeLiteral)
                        {
                            Console.Write(((Expression.LexemeTypeLiteral)variable).Literal);
                        }
                    }
                }
            }

            return new Stmt.Print();
        }

        public Stmt VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.expr != null)
            {
                value = Evaluate(stmt.expr);
            }

            throw new Return(value);
        }

        public Stmt VisitVarStmt(Stmt.Var stmt)
        {
            object value = null;
            if (stmt.isInitalizedVar != null)
            {
                value = Evaluate(stmt.exprInitializer);
            }

            environment.Define(stmt.name.ToString() , value);
            return null;
        }

        public Stmt VisitWhileStmt(Stmt.While stmt)
        {
            throw new NotImplementedException();
        }

        private object Evaluate(Expression expr)
        {
            return expr.Accept(this);
        }

        public object VisitAssignExpr(Expression.Assign expr)
        {
            object value = Evaluate(expr);
            /* Statements and State visit-assign < Resolving and Binding resolved-assign
                environment.assign(expr.name, value);
            */
            //> Resolving and Binding resolved-assign

            locals.TryGetValueEx(expr, out int? distance);
            if (distance != null)
            {
                environment.AssignAt(distance.Value, expr.name, value);
            }
            else
            {
                globals.Assign(expr.name, value);
            }

            //< Resolving and Binding resolved-assign
            return value;
        }

        public object VisitBinaryExpr(Expression.Binary expr)
        {
            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            var leftLiteralType = left as Expression.LexemeTypeLiteral;
            var rightLiteralType = right as Expression.LexemeTypeLiteral;

            if (leftLiteralType == null || rightLiteralType == null)
            {
                throw new ArgumentNullException("unable to get literal");
            }
            
            long leftValueType = leftLiteralType.lexemeType;
            long rightValueType = rightLiteralType.lexemeType;

            object leftValue = leftLiteralType.Literal;
            object rightValue = rightLiteralType.Literal;

            switch (expr.op.ToString())
            {
                case "!=" :
                        return !IsEqual(leftValue, rightValue);
                case "==":
                        return IsEqual(leftValue, rightValue);
                case ">":
                    return IsLessThan(leftValueType, rightValueType, leftValue, rightValue);
                case "/":
                    return DivideTypes(leftValueType, rightValueType, leftValue, rightValue);
                case "*":
                    return MultiplyTypes(leftValueType, rightValueType, leftValue, rightValue);
                case "-":
                    return SubtractTypes(leftValueType, rightValueType, leftValue, rightValue);
                case "+":
                    return AddTypes(leftValueType, rightValueType, leftValue, rightValue);
                //case "<":
                //case "<=":
                //case ">=":
            }

            return new Expression.LexemeTypeLiteral();
        }

        private static object IsLessThan(long leftValueType, long rightValueType, object leftValue, object rightValue)
        {
            if (leftValueType == LexemeType.NUMBER && rightValueType == LexemeType.NUMBER)
            {
                var literal = new Expression.LexemeTypeLiteral();
                literal.literal = Convert.ToInt32(leftValue) < Convert.ToInt32(rightValue);
                literal.lexemeType = LexemeType.BOOL;
                return literal;
            }

            if (leftValueType == LexemeType.STRING || rightValueType == LexemeType.STRING)
            {
                throw new ArgumentException("can't apply less than operator to strings");
            }

            throw new ArgumentException("Can't compare types");
        }

        private static object AddTypes(long leftValueType, long rightValueType, object leftValue, object rightValue)
        {
            if (leftValueType == LexemeType.NUMBER && rightValueType == LexemeType.NUMBER)
            {
                var literalSum = new Expression.LexemeTypeLiteral();
                literalSum.literal = Convert.ToInt32(leftValue) + Convert.ToInt32(rightValue);
                literalSum.lexemeType = LexemeType.NUMBER;
                return literalSum;
            }

            if (leftValueType == LexemeType.STRING && rightValueType == LexemeType.STRING)
            {
                var literalStringSum = new Expression.LexemeTypeLiteral();

                literalStringSum.literal = Convert.ToString(leftValue) + Convert.ToString(rightValue);
                literalStringSum.lexemeType = LexemeType.STRING;
                return literalStringSum;
            }

            throw new ArgumentNullException("can't add types");
        }

        private static object SubtractTypes(long leftValueType, long rightValueType, object leftValue, object rightValue)
        {
            if (leftValueType == LexemeType.NUMBER && rightValueType == LexemeType.NUMBER)
            {
                var literalSum = new Expression.LexemeTypeLiteral();
                literalSum.literal = Convert.ToInt32(leftValue) - Convert.ToInt32(rightValue);
                literalSum.lexemeType = LexemeType.NUMBER;
                return literalSum;
            }

            if (leftValueType == LexemeType.STRING && rightValueType == LexemeType.STRING)
            {
                throw new ArgumentException("can't subtract strings");
            }

            throw new ArgumentNullException("can't subtract types");
        }

        private static object MultiplyTypes(long leftValueType, long rightValueType, object leftValue, object rightValue)
        {
            if (leftValueType == LexemeType.NUMBER && rightValueType == LexemeType.NUMBER)
            {
                var literalProduction = new Expression.LexemeTypeLiteral();
                literalProduction.literal = Convert.ToInt32(leftValue) * Convert.ToInt32(rightValue);
                literalProduction.lexemeType = LexemeType.NUMBER;
                return literalProduction;
            }

            if (leftValueType == LexemeType.STRING && rightValueType == LexemeType.STRING)
            {
                throw new ArgumentException("can't multiply strings");
            }

            throw new ArgumentNullException("can't multiply types");
        }

        private static object DivideTypes(long leftValueType, long rightValueType, object leftValue, object rightValue)
        {
            if (leftValueType == LexemeType.NUMBER && rightValueType == LexemeType.NUMBER)
            {
                var literalProduction = new Expression.LexemeTypeLiteral();
                int divisor = Convert.ToInt32(leftValue);
                int divident = Convert.ToInt32(rightValue);

                if (divisor == 0 || divident == 0)
                {
                    throw new ArgumentException("Can't divide by zero");

                }
                literalProduction.literal = Convert.ToInt32(leftValue) / Convert.ToInt32(rightValue);
                literalProduction.lexemeType = LexemeType.NUMBER;
                return literalProduction;
            }

            if (leftValueType == LexemeType.STRING && rightValueType == LexemeType.STRING)
            {
                throw new ArgumentException("can't divide strings");
            }

            throw new ArgumentNullException("can't divide types");
        }

        private bool IsEqual(object a, object b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        private void CheckNumberOperand(Lexeme op, object operand)
        {
            if (operand is int)
            {
                return;
            }

            throw new ArgumentException(op.ToString() + " Operands must be numbers");
        }

        private void CheckNumberOperands(Lexeme op, object left, object right)
        {
            if (left is int && right is int )
            {
                return;
            }

            throw new ArgumentException(op.ToString() + " Operands must be numbers");
        }

        public object VisitCallExpr(Expression.Call expr)
        {
            object callee = Evaluate(expr.callee);
            List<object> arguments = new();
            foreach(Expression argument in expr.arguments)
            {
                arguments.Add(argument);
            }

            if (!(callee is IJukaCallable))
            {
                throw new ArgumentException("not a class or function");
            }

            IJukaCallable function = (IJukaCallable)callee;
            if (arguments.Count != function.Arity())
            {
                throw new ArgumentException("Wrong number of arguments");
            }

            return function.Call(this, arguments);
        }

        public object VisitGetExpr(Expression.Get expr)
        {
            throw new NotImplementedException();
        }

        public object VisitGroupingExpr(Expression.Grouping expr)
        {
            if (expr == null || expr.expression == null)
            {
                throw new ArgumentNullException("expr or expression == null");
            }

            return Evaluate(expr.expression);
        }

        public object VisitLiteralExpr(Expression.Literal expr)
        {
            return expr.LiteralValue();
        }

        public object VisitLogicalExpr(Expression.Logical expr)
        {
            throw new NotImplementedException();
        }

        public object VisitSetExpr(Expression.Set expr)
        {
            throw new NotImplementedException();
        }

        public object VisitSuperExpr(Expression.Super expr)
        {
            throw new NotImplementedException();
        }

        public object VisitThisExpr(Expression.This expr)
        {
            throw new NotImplementedException();
        }

        public object VisitUnaryExpr(Expression.Unary expr)
        {
            throw new NotImplementedException();
        }

        public object VisitVariableExpr(Expression.Variable expr)
        {
            return LookUpVariable(expr.Name, expr);
        }

        internal object LookUpVariable(Lexeme name, Expression expr)
        {
            //locals.TryGetValueEx(expr, out int? distance);
            locals.TryGetValue(expr, out int? distance);

            if (distance != null)
            {
                return environment.GetAt(distance.Value, name.ToString());
            }
            else
            {
                return globals.Get(name);
            }
        }
        internal ServiceProvider ServiceProvider
        {
            get { return this.serviceProvider; }
        }

        internal void Resolve(Expression expr, int depth)
        {
            if (locals.Where( f => f.Key.Name.ToString().Equals(expr.Name.ToString()) ).Count() <= 1)
            {
                locals.Add(expr,depth);
            }
        }

        private bool IsTrue(object o)
        {
            if (o == null)
            {
                return false;
            }

            if (o is bool)
            {
                return (bool)o;
            }

            return true;
        }
    }
}