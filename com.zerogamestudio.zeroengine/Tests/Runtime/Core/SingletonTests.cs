using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroEngine.Core;

namespace ZeroEngine.Tests.Core
{
    /// <summary>
    /// Singleton 和 PersistentSingleton 单元测试
    /// 需要 PlayMode 因为依赖 MonoBehaviour 生命周期
    /// </summary>
    [TestFixture]
    public class SingletonTests
    {
        [TearDown]
        public void TearDown()
        {
            // 清理测试创建的 Singleton 对象
            CleanupSingleton<TestSingleton>();
            CleanupSingleton<TestPersistentSingleton>();
            CleanupSingleton<TestSingleton2>();
        }

        private void CleanupSingleton<T>() where T : MonoBehaviour
        {
            var obj = GameObject.Find($"[{typeof(T).Name}]");
            if (obj != null) Object.DestroyImmediate(obj);
        }

        #region Singleton Tests

        [UnityTest]
        public IEnumerator Singleton_Instance_CreatesGameObject()
        {
            // Act
            var instance = TestSingleton.Instance;
            yield return null;

            // Assert
            Assert.IsNotNull(instance);
            Assert.IsNotNull(GameObject.Find("[TestSingleton]"));
        }

        [UnityTest]
        public IEnumerator Singleton_Instance_ReturnsSameInstance()
        {
            // Act
            var instance1 = TestSingleton.Instance;
            var instance2 = TestSingleton.Instance;
            yield return null;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [UnityTest]
        public IEnumerator Singleton_DuplicateInstance_DestroysNew()
        {
            // Arrange
            var original = TestSingleton.Instance;
            yield return null;

            // Act - 手动创建第二个实例
            var duplicateGO = new GameObject("DuplicateSingleton");
            var duplicate = duplicateGO.AddComponent<TestSingleton>();
            yield return null;

            // Assert
            Assert.AreSame(original, TestSingleton.Instance);
            // 重复的应该被销毁 (在下一帧)
            yield return null;
            Assert.IsTrue(duplicateGO == null || !duplicateGO.activeInHierarchy || duplicate == null);
        }

        [UnityTest]
        public IEnumerator Singleton_OnDestroy_ClearsInstance()
        {
            // Arrange
            var instance = TestSingleton.Instance;
            var go = instance.gameObject;
            yield return null;

            // Act
            Object.DestroyImmediate(go);
            yield return null;

            // 创建新实例验证可以重新创建
            var newInstance = TestSingleton.Instance;
            yield return null;

            // Assert
            Assert.IsNotNull(newInstance);
            Assert.AreNotSame(instance, newInstance);
        }

        #endregion

        #region PersistentSingleton Tests

        [UnityTest]
        public IEnumerator PersistentSingleton_Instance_CreatesGameObject()
        {
            // Act
            var instance = TestPersistentSingleton.Instance;
            yield return null;

            // Assert
            Assert.IsNotNull(instance);
            Assert.IsNotNull(GameObject.Find("[TestPersistentSingleton]"));
        }

        [UnityTest]
        public IEnumerator PersistentSingleton_Instance_ReturnsSameInstance()
        {
            // Act
            var instance1 = TestPersistentSingleton.Instance;
            var instance2 = TestPersistentSingleton.Instance;
            yield return null;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [UnityTest]
        public IEnumerator PersistentSingleton_MarkedDontDestroyOnLoad()
        {
            // Act
            var instance = TestPersistentSingleton.Instance;
            yield return null;

            // Assert - DontDestroyOnLoad 的对象会在特殊场景中
            // 验证对象存在且在 DontDestroyOnLoad 场景
            Assert.IsNotNull(instance);
            Assert.IsNotNull(instance.gameObject);
            // 验证对象的场景名 (DontDestroyOnLoad 对象的 scene.name 为空或特殊)
            Assert.IsTrue(
                instance.gameObject.scene.name == "DontDestroyOnLoad" ||
                instance.gameObject.scene.buildIndex == -1 ||
                !instance.gameObject.scene.isLoaded
            );
        }

        #endregion

        #region MonoSingleton Alias Tests

        [UnityTest]
        public IEnumerator MonoSingleton_IsAliasForSingleton()
        {
            // Act
            var instance = TestMonoSingleton.Instance;
            yield return null;

            // Assert
            Assert.IsNotNull(instance);
            Assert.IsNotNull(GameObject.Find("[TestMonoSingleton]"));
        }

        #endregion
    }

    #region Test Singleton Classes

    /// <summary>
    /// 测试用普通 Singleton
    /// </summary>
    public class TestSingleton : Singleton<TestSingleton>
    {
        public int TestValue { get; set; }
    }

    /// <summary>
    /// 测试用持久化 Singleton
    /// </summary>
    public class TestPersistentSingleton : PersistentSingleton<TestPersistentSingleton>
    {
        public string TestData { get; set; }
    }

    /// <summary>
    /// 测试用 MonoSingleton (别名验证)
    /// </summary>
    public class TestMonoSingleton : MonoSingleton<TestMonoSingleton>
    {
    }

    /// <summary>
    /// 第二个测试 Singleton (用于隔离测试)
    /// </summary>
    public class TestSingleton2 : Singleton<TestSingleton2>
    {
    }

    #endregion
}
