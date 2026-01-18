using System.Collections.Generic;
using NUnit.Framework;
using ZeroEngine.Performance.Collections;

namespace ZeroEngine.Tests.Core
{
    /// <summary>
    /// ListPool 对象池单元测试
    /// </summary>
    [TestFixture]
    public class ListPoolTests
    {
        [SetUp]
        public void SetUp()
        {
            // 清理池状态
            ListPool<int>.Clear();
            ListPool<int>.ResetStats();
            ListPool<string>.Clear();
            ListPool<string>.ResetStats();
        }

        #region Get/Return Tests

        [Test]
        public void Get_ReturnsListInstance()
        {
            // Act
            var list = ListPool<int>.Get();

            // Assert
            Assert.IsNotNull(list);
            Assert.IsInstanceOf<List<int>>(list);

            // Cleanup
            ListPool<int>.Return(list);
        }

        [Test]
        public void Get_ReturnedList_IsEmpty()
        {
            // Arrange
            var list = ListPool<int>.Get();
            list.Add(1);
            list.Add(2);
            ListPool<int>.Return(list);

            // Act
            var reusedList = ListPool<int>.Get();

            // Assert
            Assert.AreEqual(0, reusedList.Count);

            // Cleanup
            ListPool<int>.Return(reusedList);
        }

        [Test]
        public void Return_AddsToPool()
        {
            // Arrange
            var list = ListPool<int>.Get();
            int initialCount = ListPool<int>.PooledCount;

            // Act
            ListPool<int>.Return(list);

            // Assert
            Assert.AreEqual(initialCount + 1, ListPool<int>.PooledCount);
        }

        [Test]
        public void Get_AfterReturn_ReusesInstance()
        {
            // Arrange
            var list1 = ListPool<int>.Get();
            ListPool<int>.Return(list1);

            // Act
            var list2 = ListPool<int>.Get();

            // Assert
            Assert.AreSame(list1, list2);

            // Cleanup
            ListPool<int>.Return(list2);
        }

        #endregion

        #region Stats Tests

        [Test]
        public void GetStats_TracksGetCount()
        {
            // Act
            var list1 = ListPool<int>.Get();
            var list2 = ListPool<int>.Get();
            var list3 = ListPool<int>.Get();

            // Assert
            Assert.AreEqual(3, ListPool<int>.GetCount);

            // Cleanup
            ListPool<int>.Return(list1);
            ListPool<int>.Return(list2);
            ListPool<int>.Return(list3);
        }

        [Test]
        public void GetStats_TracksReturnCount()
        {
            // Arrange
            var list1 = ListPool<int>.Get();
            var list2 = ListPool<int>.Get();

            // Act
            ListPool<int>.Return(list1);
            ListPool<int>.Return(list2);

            // Assert
            Assert.AreEqual(2, ListPool<int>.ReturnCount);
        }

        [Test]
        public void GetStats_ReturnsCorrectActiveCount()
        {
            // Arrange
            var list1 = ListPool<int>.Get();
            var list2 = ListPool<int>.Get();
            ListPool<int>.Return(list1);

            // Act
            var stats = ListPool<int>.GetStats();

            // Assert
            Assert.AreEqual(1, stats.ActiveCount); // 2 get - 1 return = 1 active

            // Cleanup
            ListPool<int>.Return(list2);
        }

        [Test]
        public void ResetStats_ClearsAllCounters()
        {
            // Arrange
            var list = ListPool<int>.Get();
            ListPool<int>.Return(list);

            // Act
            ListPool<int>.ResetStats();

            // Assert
            Assert.AreEqual(0, ListPool<int>.GetCount);
            Assert.AreEqual(0, ListPool<int>.ReturnCount);
            Assert.AreEqual(0, ListPool<int>.TotalCreated);
        }

        #endregion

        #region WarmUp Tests

        [Test]
        public void WarmUp_PreCreatesInstances()
        {
            // Act
            ListPool<string>.WarmUp(5);

            // Assert
            Assert.AreEqual(5, ListPool<string>.PooledCount);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_EmptiesPool()
        {
            // Arrange
            ListPool<int>.WarmUp(3);

            // Act
            ListPool<int>.Clear();

            // Assert
            Assert.AreEqual(0, ListPool<int>.PooledCount);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Return_NullList_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => ListPool<int>.Return(null));
        }

        [Test]
        public void Get_WithCapacity_CreatesWithCapacity()
        {
            // Act
            var list = ListPool<int>.Get(100);

            // Assert
            Assert.GreaterOrEqual(list.Capacity, 100);

            // Cleanup
            ListPool<int>.Return(list);
        }

        #endregion
    }
}
