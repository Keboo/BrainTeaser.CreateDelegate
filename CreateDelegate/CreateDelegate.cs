using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace CreateDelegate
{
    [TestClass]
    public class CreateDelegate
    {
        private struct MyStruct
        {
            public string Value { get; set; }
        }

        private delegate bool HasOutParameter(out int foo);
        private delegate bool HasRefParameter(ref int foo);
        private delegate T3 HasGeneric<in T1, in T2, out T3>(T1 one, T2 two);

        [TestMethod]
        public void WorksWithAction()
        {
            Action @delegate = (Action) CreateEmptyDelegate(typeof(Action));
            @delegate();
        }

        [TestMethod]
        public void WorksWithActionParameters()
        {
            Action<int> @delegate = (Action<int>)CreateEmptyDelegate(typeof(Action<int>));
            @delegate(42);
        }

        [TestMethod]
        public void WorksWithIntReturn()
        {
            Func<int> @delegate = (Func<int>)CreateEmptyDelegate(typeof(Func<int>));
            Assert.AreEqual(0, @delegate());
        }

        [TestMethod]
        public void WorksWithStructReturn()
        {
            Func<MyStruct> @delegate = (Func<MyStruct>)CreateEmptyDelegate(typeof(Func<MyStruct>));
            Assert.AreEqual(default, @delegate());
        }

        [TestMethod]
        public void WorksWithClassReturn()
        {
            Func<object> @delegate = (Func<object>)CreateEmptyDelegate(typeof(Func<object>));
            Assert.AreEqual(null, @delegate());
        }

        [TestMethod]
        public async Task WorksWithAsyncMethod()
        {
            Func<Task> @delegate = (Func<Task>)CreateEmptyDelegate(typeof(Func<Task>));
            await @delegate();
        }

        [TestMethod]
        public async Task WorksWithAsyncMethodWithReturn()
        {
            Func<Task<int>> @delegate = (Func<Task<int>>)CreateEmptyDelegate(typeof(Func<Task<int>>));
            Assert.AreEqual(0, await @delegate());
        }

        [TestMethod]
        public async Task WorksWithAsyncMethodWithStructReturn()
        {
            Func<Task<MyStruct>> @delegate = (Func<Task<MyStruct>>)CreateEmptyDelegate(typeof(Func<Task<MyStruct>>));
            Assert.AreEqual(default, await @delegate());
        }

        [TestMethod]
        public async Task WorksWithAsyncMethodWithClassReturn()
        {
            Func<Task<object>> @delegate = (Func<Task<object>>)CreateEmptyDelegate(typeof(Func<Task<object>>));
            Assert.AreEqual(null, await @delegate());
        }

        [TestMethod]
        public void WorksWithOutParameter()
        {
            HasOutParameter @delegate = (HasOutParameter)CreateEmptyDelegate(typeof(HasOutParameter));
            Assert.IsFalse(@delegate(out int intValue));
            Assert.AreEqual(0, intValue);
        }

        [TestMethod]
        public void WorksWithRefParameter()
        {
            int foo = 42;
            HasRefParameter @delegate = (HasRefParameter)CreateEmptyDelegate(typeof(HasRefParameter));
            Assert.IsFalse(@delegate(ref foo));
            Assert.AreEqual(42, foo);
        }

        [TestMethod]
        public void WorksWithGenericDelegate()
        {
            HasGeneric<double, byte, decimal> @delegate = (HasGeneric<double, byte, decimal>)CreateEmptyDelegate(typeof(HasGeneric<double, byte, decimal>));
            Assert.AreEqual(default, @delegate(default, default));
        }

        public static Delegate CreateEmptyDelegate(Type delegateType)
        {
            if (delegateType == null) throw new ArgumentNullException(nameof(delegateType));
            if (!delegateType.IsSubclassOf(typeof(Delegate)))
            {
                throw new ArgumentException($"{delegateType.FullName} is not a delegate type");
            }

            MethodInfo mi = delegateType.GetMethod("Invoke");

            var dm = new DynamicMethod($"<AutoGen_{delegateType.FullName}>", mi.ReturnType, mi.GetParameters().Select(x => x.ParameterType).ToArray());

            ILGenerator generator = dm.GetILGenerator();

            if (mi.ReturnType == typeof(void))
            { }
            else if (mi.ReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Ldc_I4_0);
            }
            else if (mi.ReturnType == typeof(Task))
            {
                generator.Emit(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask)).GetMethod);
            }
            else if (mi.ReturnType.IsGenericType && mi.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskReturnType = mi.ReturnType.GetGenericArguments().First();
                //This also works for reference types:
                //"Note that object references and pointer types can be assigned the value null. This is defined throughout the
                // CLI to be zero(a bit pattern of all-bits - zero)."
                // See http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20III.pdf
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Call, typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(taskReturnType));
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }
            generator.Emit(OpCodes.Ret);

            return dm.CreateDelegate(delegateType);
        }
    }
}
