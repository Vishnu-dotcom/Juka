﻿using JukaCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace JukaUnitTest
{
    [TestClass]
    public class CompilerUnitTest
    {
        [TestMethod]
        public void TestEmpty()
        {
            try
            {
                Compiler compiler = new Compiler();
                compiler.Go("", @"D:\github\Juka\examples\test.jlr");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        public void TestEmptyFunc()
        {
            try
            {
                Compiler compiler = new Compiler();
                compiler.Go("", "function main() =  {}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        public void TestBrokenToken()
        {
            try
            {
                Compiler compiler = new Compiler();
                compiler.Go("", "asdfunct main() =  {");
            }
            catch (Exception)
            {
                throw;
            }
        }

        /*[TestMethod]
        public void TestEmptyFunc()
        {
            var mockScanner = new Mock<IScanner>();
            var tokenArray = StringToToken("func(){}");

            int bufferCount = -1;

            mockScanner.Setup(f => f.ReadToken() )
                .Returns(()=>
            {
                bufferCount++;

                if (bufferCount == tokenArray.Length)
                {
                    return new Token(TokenType.Eof);
                }

                return tokenArray[bufferCount];
            });

            mockScanner.Setup(f => f.PutTokenBack()).Callback(() => 
            {
                bufferCount--;
            });

            try
            {
                int sequenceCount = 5;

                ILexicalAnalysis lexical = new LexicalAnalysis();
                var llm = lexical.Analyze(mockScanner.Object);

                Assert.IsNotNull(llm);
                Assert.AreEqual(sequenceCount, llm.Count, $"Lexem count is off. Ensure the Token Array is accurate.");
            }
            catch (Exception)
            {
                throw;
            }
        }
        

        private Token[] StringToToken(String tokenString)
        {
            var tokenArray = new Token[tokenString.Length];
            for (int counter = 0; counter < tokenString.Length; counter++)
            {
                if (Char.IsLetter(tokenString[counter]))
                {
                    tokenArray[counter] = new Token(TokenType.Character, tokenString[counter]);
                }
                else
                {
                    tokenArray[counter] = new Token(TokenType.Symbol, tokenString[counter]);
                }
            }
            return tokenArray;
        }*/

    }
}