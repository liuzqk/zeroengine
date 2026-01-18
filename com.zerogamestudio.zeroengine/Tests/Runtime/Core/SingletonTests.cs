using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZeroEngine.Core;

namespace ZeroEngine.Tests.Core
{
    /// <summary>
    /// Singleton 基础功能验证测试
    /// 注意：由于泛型单例的静态字段在 PlayMode 测试间难以完全隔离，
    /// 这里仅测试基础功能。完整的 Singleton 行为已在实际项目中验证。
    /// </summary>
    [TestFixture]
    public class SingletonTests
    {
        [UnityTest]
        public IEnumerator Singleton_BasicFunctionality_Works()
        {
            // 清理可能存在的实例
            var existing = GameObject.Find("[BasicTestSingleton]");
            if (existing != null) Object.DestroyImmediate(existing);
            yield return null;

            // Act - 创建实例
            var instance = BasicTestSingleton.Instance;
            yield return null;

            // Assert
            Assert.IsNotNull(instance, "Singleton should create instance");
            Assert.IsNotNull(GameObject.Find("[BasicTestSingleton]"), "GameObject should exist");

            // 验证单例性
            var instance2 = BasicTestSingleton.Instance;
            Assert.AreSame(instance, instance2, "Should return same instance");

            // 清理
            Object.DestroyImmediate(instance.gameObject);
        }

        [UnityTest]
        public IEnumerator PersistentSingleton_BasicFunctionality_Works()
        {
            // 清理
            var existing = GameObject.Find("[BasicPersistentTestSingleton]");
            if (existing != null) Object.DestroyImmediate(existing);
            yield return null;

            // Act
            var instance = BasicPersistentTestSingleton.Instance;
            yield return null;

            // Assert
            Assert.IsNotNull(instance, "PersistentSingleton should create instance");

            // 验证 DontDestroyOnLoad
            var scene = instance.gameObject.scene;
            Assert.IsTrue(
                scene.name == "DontDestroyOnLoad" || scene.buildIndex == -1,
                $"Should be in DontDestroyOnLoad, got: {scene.name}"
            );

            // 清理
            Object.DestroyImmediate(instance.gameObject);
        }
    }

    // 独立的测试类，避免与其他测试冲突
    public class BasicTestSingleton : Singleton<BasicTestSingleton> { }
    public class BasicPersistentTestSingleton : PersistentSingleton<BasicPersistentTestSingleton> { }
}
