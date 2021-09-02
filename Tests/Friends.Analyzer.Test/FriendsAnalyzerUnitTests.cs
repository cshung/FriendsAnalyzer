namespace Friends.Analyzer.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading.Tasks;
    using VerifyCS = Friends.Analyzer.Test.CSharpCodeFixVerifier<
        Friends.Analyzer.FriendsAnalyzer,
        Friends.Analyzer.FriendsAnalyzerCodeFixProvider>;


    [TestClass]
    public class FriendsAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        class FriendsAttribute : Attribute
        {
            private Type type;

            public FriendsAttribute(Type type)
            {
                this.type = type;
            }

            public Type Type
            {
                get
                {
                    return this.type;
                }
            }
        }
        class TypeName
        {
            [Friends(typeof(Friend))]
            public static void WriteLine()
            {
            }

            public static void Bing()
            {
                WriteLine();
            }
        }
        class Friend
        {
            public static void Bang()
            {
                TypeName.WriteLine();
            }
        }
        class Stranger
        {
            public static void Bang()
            {
                {|#0:TypeName.WriteLine()|};
            }
        }
    }";
            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        class FriendsAttribute : Attribute
        {
            private Type type;

            public FriendsAttribute(Type type)
            {
                this.type = type;
            }

            public Type Type
            {
                get
                {
                    return this.type;
                }
            }
        }
        class TypeName
        {
        [Friends(typeof(Friend))]
        [Friends(typeof(ConsoleApplication1.Stranger))]
        public static void WriteLine()
        {
        }

        public static void Bing()
            {
                WriteLine();
            }
        }
        class Friend
        {
            public static void Bang()
            {
                TypeName.WriteLine();
            }
        }
        class Stranger
        {
            public static void Bang()
            {
                {|#0:TypeName.WriteLine()|};
            }
        }
    }";

            var expected = VerifyCS.Diagnostic("FriendsAnalyzer").WithLocation(0).WithArguments("TypeName.WriteLine()");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
