using System;
using NUnit.Framework;

namespace ZeroEngine.Tests
{
    /// <summary>
    /// 测试辅助工具类
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// 断言动作会抛出指定类型的异常
        /// </summary>
        public static T AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail($"Expected exception of type {typeof(T).Name} but no exception was thrown.");
                return null;
            }
            catch (T ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 断言两个浮点数近似相等
        /// </summary>
        public static void AssertApproximatelyEqual(float expected, float actual, float tolerance = 0.0001f)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance),
                $"Expected {expected} but got {actual} (tolerance: {tolerance})");
        }

        /// <summary>
        /// 断言事件被触发指定次数
        /// </summary>
        public static EventCounter CreateEventCounter()
        {
            return new EventCounter();
        }
    }

    /// <summary>
    /// 事件计数器，用于验证事件触发次数
    /// </summary>
    public class EventCounter
    {
        public int Count { get; private set; }

        public void Increment()
        {
            Count++;
        }

        public void Increment<T>(T _)
        {
            Count++;
        }

        public void Reset()
        {
            Count = 0;
        }

        public void AssertCount(int expected)
        {
            Assert.AreEqual(expected, Count, $"Expected event to be triggered {expected} times but was {Count} times.");
        }
    }
}
