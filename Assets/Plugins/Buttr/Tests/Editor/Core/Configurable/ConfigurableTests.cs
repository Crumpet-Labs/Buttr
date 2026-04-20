using System;
using Buttr.Core;
using NUnit.Framework;

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable RedundantBoolCompare

namespace Buttr.Tests.Editor.Configurable {
    public sealed class ConfigurableTests {
        class TestConfigurable<T> : IConfigurable<T> {
            public bool ConfigurationSet { get; private set; } = false;
            public bool FactorySet { get; private set; } = false;

            public Func<T, T> LastConfiguration { get; private set; }
            public Func<T> LastFactory { get; private set; }

            public IConfigurable<T> WithConfiguration(Func<T, T> configuration) {
                ConfigurationSet = true;
                LastConfiguration = configuration;
                return this;
            }

            public IConfigurable<T> WithFactory(Func<T> factory) {
                FactorySet = true;
                LastFactory = factory;
                return this;
            }

            public IConfigurable<T> As<TAlias>() => this;
        }

        private ConfigurableCollection m_Collection;

        [SetUp]
        public void Setup() {
            m_Collection = new ConfigurableCollection();
        }
        
        [Test]
        public void ConfigurableCollection_Register_ShouldStoreConfigurableByType() {
            var configurable = new TestConfigurable<string>();

            var returned = m_Collection.Register(configurable);
            Assert.AreSame(m_Collection, returned, "Register should return self for chaining");

            // Internals can't be accessed directly, so test by calling WithConfiguration later
            Assert.DoesNotThrow(() => m_Collection.WithConfiguration<string>(s => s));
        }

        [Test]
        public void ConfigurableCollection_WithConfiguration_ShouldSetConfigurationOfConfigurable() {
            var configurable = new TestConfigurable<string>();
            Func<string, string> func = s => { return s + " : "; };
            m_Collection.Register(configurable);
            
            Assert.IsTrue(configurable.ConfigurationSet == false);
            m_Collection.WithConfiguration<string>(func);
            Assert.IsTrue(configurable.ConfigurationSet == true);
        }
        
        [Test]
        public void ConfigurableCollection_WithFactory_ShouldSetFactoryOfConfigurable() {
            var configurable = new TestConfigurable<string>();
            Func<string> func = () => { return "Factory"; };
            m_Collection.Register(configurable);
            
            Assert.IsTrue(configurable.FactorySet == false);
            m_Collection.WithFactory<string>(func);
            Assert.IsTrue(configurable.FactorySet == true);
        }

        [Test]
        public void ConfigurableCollection_WithConfiguration_DoesNotInvokeOnRegisteredConfigurable() {
            var configurable = new TestConfigurable<double>();
            m_Collection.Register(configurable);

            var configCalled = false;
            Func<double, double> configFunc = (_) => {
                configCalled = true;
                return 3.14;
            };

            var returned = m_Collection.WithConfiguration(configFunc);

            Assert.IsTrue(configurable.ConfigurationSet, "WithConfiguration should call the configurable's WithConfiguration");
            Assert.AreSame(configFunc, configurable.LastConfiguration, "Configuration func should match");
            Assert.AreSame(m_Collection, returned, "WithConfiguration should return self for chaining");

            // Calling factory to verify it works
            var value = configurable.LastConfiguration?.Invoke(0.0);
            Assert.AreEqual(3.14, value);
            Assert.IsTrue(configCalled);
        }

        [Test]
        public void ConfigurableCollection_WithFactory_DoesNotInvokeOnRegisteredConfigurable() {
            var configurable = new TestConfigurable<double>();
            m_Collection.Register(configurable);

            bool factoryCalled = false;
            Func<double> factoryFunc = () => {
                factoryCalled = true;
                return 3.14;
            };

            var returned = m_Collection.WithFactory(factoryFunc);

            Assert.IsTrue(configurable.FactorySet, "WithFactory should call the configurable's WithFactory");
            Assert.AreSame(factoryFunc, configurable.LastFactory, "Factory func should match");
            Assert.AreSame(m_Collection, returned, "WithFactory should return self for chaining");

            // Calling factory to verify it works
            var value = configurable.LastFactory?.Invoke();
            Assert.AreEqual(3.14, value);
            Assert.IsTrue(factoryCalled);
        }

        [Test]
        public void ConfigurableCollection_WithConfiguration_WhenTypeNotRegistered_ShouldThrow() {
            Assert.Throws<ConfigurableException>(() => { m_Collection.WithConfiguration<Guid>(x => x); } );
        }

        [Test]
        public void ConfigurableCollection_WithFactory_WhenTypeNotRegistered_ShouldThrow() {
            Assert.Throws<ConfigurableException>(() => { m_Collection.WithFactory<Guid>(() => Guid.Empty); } );
        }

        [Test]
        public void ConfigurableCollection_RegisteringSameServiceTwiceThrows() {
            var configurable1 = new TestConfigurable<double>();
            var configurable2 = new TestConfigurable<double>();

            m_Collection.Register(configurable1);

            Assert.Throws<ArgumentException>(() => m_Collection.Register(configurable2));
        }
    }
}