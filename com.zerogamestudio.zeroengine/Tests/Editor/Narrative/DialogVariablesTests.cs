using NUnit.Framework;
using ZeroEngine.Dialog;

namespace ZeroEngine.Tests.Narrative
{
    /// <summary>
    /// DialogVariables 单元测试
    /// </summary>
    [TestFixture]
    public class DialogVariablesTests
    {
        private DialogVariables _variables;

        [SetUp]
        public void SetUp()
        {
            _variables = new DialogVariables();
            _variables.ClearLocal();
            DialogVariables.ClearGlobal();
        }

        #region DialogVariable Struct Tests

        [Test]
        public void DialogVariable_Create_Bool()
        {
            // Act
            var variable = DialogVariable.Create("test", true);

            // Assert
            Assert.AreEqual("test", variable.Name);
            Assert.AreEqual(DialogVariableType.Bool, variable.Type);
            Assert.IsTrue(variable.GetBool());
        }

        [Test]
        public void DialogVariable_Create_Int()
        {
            // Act
            var variable = DialogVariable.Create("count", 42);

            // Assert
            Assert.AreEqual(DialogVariableType.Int, variable.Type);
            Assert.AreEqual(42, variable.GetInt());
        }

        [Test]
        public void DialogVariable_Create_Float()
        {
            // Act
            var variable = DialogVariable.Create("ratio", 3.14f);

            // Assert
            Assert.AreEqual(DialogVariableType.Float, variable.Type);
            Assert.AreEqual(3.14f, variable.GetFloat(), 0.001f);
        }

        [Test]
        public void DialogVariable_Create_String()
        {
            // Act
            var variable = DialogVariable.Create("name", "Alice");

            // Assert
            Assert.AreEqual(DialogVariableType.String, variable.Type);
            Assert.AreEqual("Alice", variable.GetString());
        }

        [Test]
        public void DialogVariable_IsTruthy_Bool()
        {
            Assert.IsTrue(DialogVariable.Create("t", true).IsTruthy());
            Assert.IsFalse(DialogVariable.Create("f", false).IsTruthy());
        }

        [Test]
        public void DialogVariable_IsTruthy_Int()
        {
            Assert.IsTrue(DialogVariable.Create("n", 1).IsTruthy());
            Assert.IsTrue(DialogVariable.Create("n", -1).IsTruthy());
            Assert.IsFalse(DialogVariable.Create("n", 0).IsTruthy());
        }

        [Test]
        public void DialogVariable_IsTruthy_String()
        {
            Assert.IsTrue(DialogVariable.Create("s", "hello").IsTruthy());
            Assert.IsFalse(DialogVariable.Create("s", "").IsTruthy());
            Assert.IsFalse(DialogVariable.Create("s", (string)null).IsTruthy());
        }

        #endregion

        #region Local Variables Tests

        [Test]
        public void SetLocal_GetLocal_ReturnsCorrectValue()
        {
            // Act
            _variables.SetLocal("score", 100);

            // Assert
            Assert.AreEqual(100, _variables.GetLocal<int>("score"));
        }

        [Test]
        public void HasLocal_ReturnsTrueForExisting()
        {
            // Arrange
            _variables.SetLocal("exists", true);

            // Assert
            Assert.IsTrue(_variables.HasLocal("exists"));
            Assert.IsFalse(_variables.HasLocal("notExists"));
        }

        [Test]
        public void ClearLocal_RemovesAllLocalVariables()
        {
            // Arrange
            _variables.SetLocal("a", 1);
            _variables.SetLocal("b", 2);

            // Act
            _variables.ClearLocal();

            // Assert
            Assert.IsFalse(_variables.HasLocal("a"));
            Assert.IsFalse(_variables.HasLocal("b"));
        }

        [Test]
        public void SetLocal_FiresOnVariableChanged()
        {
            // Arrange
            string changedName = null;
            object oldValue = null;
            object newValue = null;
            _variables.OnVariableChanged += (name, old, @new) =>
            {
                changedName = name;
                oldValue = old;
                newValue = @new;
            };

            // Act
            _variables.SetLocal("level", 5);

            // Assert
            Assert.AreEqual("level", changedName);
            Assert.IsNull(oldValue);
            Assert.AreEqual(5, newValue);
        }

        #endregion

        #region Global Variables Tests

        [Test]
        public void SetGlobal_GetGlobal_ReturnsCorrectValue()
        {
            // Act
            DialogVariables.SetGlobal("playerName", "Bob");

            // Assert
            Assert.AreEqual("Bob", DialogVariables.GetGlobal<string>("playerName"));
        }

        [Test]
        public void HasGlobal_ReturnsTrueForExisting()
        {
            // Arrange
            DialogVariables.SetGlobal("global", 42);

            // Assert
            Assert.IsTrue(DialogVariables.HasGlobal("global"));
            Assert.IsFalse(DialogVariables.HasGlobal("notGlobal"));
        }

        [Test]
        public void ClearGlobal_RemovesAllGlobalVariables()
        {
            // Arrange
            DialogVariables.SetGlobal("x", 1);
            DialogVariables.SetGlobal("y", 2);

            // Act
            DialogVariables.ClearGlobal();

            // Assert
            Assert.IsFalse(DialogVariables.HasGlobal("x"));
            Assert.IsFalse(DialogVariables.HasGlobal("y"));
        }

        #endregion

        #region Unified Access Tests

        [Test]
        public void Get_LocalFirst_ThenGlobal()
        {
            // Arrange
            _variables.SetLocal("shared", "local");
            DialogVariables.SetGlobal("shared", "global");

            // Act & Assert - Local takes precedence
            Assert.AreEqual("local", _variables.Get("shared"));
        }

        [Test]
        public void Get_FallsBackToGlobal()
        {
            // Arrange
            DialogVariables.SetGlobal("onlyGlobal", 999);

            // Act & Assert
            Assert.AreEqual(999, _variables.Get<int>("onlyGlobal"));
        }

        [Test]
        public void Get_GlobalPrefix_AccessesGlobalDirectly()
        {
            // Arrange
            _variables.SetLocal("value", "local");
            DialogVariables.SetGlobal("value", "global");

            // Act - $ prefix forces global access
            var result = _variables.Get("$value");

            // Assert
            Assert.AreEqual("global", result);
        }

        [Test]
        public void Set_GlobalPrefix_SetsGlobal()
        {
            // Act
            _variables.Set("$globalVar", 123);

            // Assert
            Assert.AreEqual(123, DialogVariables.GetGlobal<int>("globalVar"));
        }

        [Test]
        public void Has_ChecksBothScopes()
        {
            // Arrange
            _variables.SetLocal("localOnly", true);
            DialogVariables.SetGlobal("globalOnly", true);

            // Assert
            Assert.IsTrue(_variables.Has("localOnly"));
            Assert.IsTrue(_variables.Has("globalOnly"));
            Assert.IsFalse(_variables.Has("neither"));
        }

        #endregion

        #region IsTruthy Tests

        [Test]
        public void IsTruthy_EvaluatesLocalVariable()
        {
            // Arrange
            _variables.SetLocal("active", true);
            _variables.SetLocal("inactive", false);

            // Assert
            Assert.IsTrue(_variables.IsTruthy("active"));
            Assert.IsFalse(_variables.IsTruthy("inactive"));
        }

        [Test]
        public void IsTruthy_EvaluatesGlobalWithPrefix()
        {
            // Arrange
            DialogVariables.SetGlobal("flag", 1);

            // Act & Assert
            Assert.IsTrue(_variables.IsTruthy("$flag"));
        }

        [Test]
        public void IsTruthyValue_EvaluatesCorrectly()
        {
            Assert.IsTrue(DialogVariables.IsTruthyValue(true));
            Assert.IsFalse(DialogVariables.IsTruthyValue(false));
            Assert.IsTrue(DialogVariables.IsTruthyValue(1));
            Assert.IsFalse(DialogVariables.IsTruthyValue(0));
            Assert.IsTrue(DialogVariables.IsTruthyValue("text"));
            Assert.IsFalse(DialogVariables.IsTruthyValue(""));
            Assert.IsFalse(DialogVariables.IsTruthyValue(null));
        }

        #endregion

        #region Export/Import Tests

        [Test]
        public void ExportLocal_ReturnsAllLocalVariables()
        {
            // Arrange
            _variables.SetLocal("a", 1);
            _variables.SetLocal("b", "text");

            // Act
            var exported = _variables.ExportLocal();

            // Assert
            Assert.AreEqual(2, exported.Count);
            Assert.AreEqual(1, exported["a"]);
            Assert.AreEqual("text", exported["b"]);
        }

        [Test]
        public void ImportLocal_RestoresVariables()
        {
            // Arrange
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "imported", 42 },
                { "name", "Test" }
            };

            // Act
            _variables.ImportLocal(data);

            // Assert
            Assert.AreEqual(42, _variables.GetLocal<int>("imported"));
            Assert.AreEqual("Test", _variables.GetLocal<string>("name"));
        }

        [Test]
        public void ExportGlobal_ReturnsAllGlobalVariables()
        {
            // Arrange
            DialogVariables.SetGlobal("g1", 100);
            DialogVariables.SetGlobal("g2", true);

            // Act
            var exported = DialogVariables.ExportGlobal();

            // Assert
            Assert.AreEqual(2, exported.Count);
        }

        #endregion
    }
}
