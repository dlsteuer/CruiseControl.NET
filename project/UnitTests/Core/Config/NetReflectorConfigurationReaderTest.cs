using System;
using System.Collections;
using System.Xml;
using Exortech.NetReflector;
using NUnit.Framework;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Core.Config;
using ThoughtWorks.CruiseControl.Core.Security;
using ThoughtWorks.CruiseControl.Core.Sourcecontrol;
using ThoughtWorks.CruiseControl.Core.Tasks;
using ThoughtWorks.CruiseControl.Core.Util;
using ThoughtWorks.CruiseControl.Remote;
using System.Collections.Generic;

namespace ThoughtWorks.CruiseControl.UnitTests.Core.Config
{
	[TestFixture]
	public class NetReflectorConfigurationReaderTest
	{
		private NetReflectorConfigurationReader reader;

		[SetUp]
		protected void CreateReader()
		{
			reader = new NetReflectorConfigurationReader();
		}

		[Test]
		public void DeserialiseSingleProjectFromXml()
		{
			string projectXml = ConfigurationFixture.GenerateProjectXml("test");
			IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml), null);
			ValidateProject(configuration, "test");
		}

        [Test]
        public void DeserialiseSingleProjectPlusQueueFromXml()
        {
            string projectXml = ConfigurationFixture.GenerateProjectXml("test");
            string queueXml = "<queue name=\"test\" duplicates=\"ApplyForceBuildsReAdd\"/>";
            IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml+queueXml), null);
            ValidateProject(configuration, "test");
        }

        [Test]
        public void DeserialiseSecurityFromXml()
        {
            string projectXml = ConfigurationFixture.GenerateProjectXml("test");
            string queueXml = "<nullSecurity/>";
            IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml + queueXml), null);
            Assert.IsInstanceOfType(typeof(NullSecurityManager), configuration.SecurityManager);
        }

        [Test]
        [ExpectedException(typeof(ConfigurationException))]
        public void DeserialiseSingleProjectPlusUnknownFromXml()
        {
            string projectXml = ConfigurationFixture.GenerateProjectXml("test");
            string queueXml = "<garbage/>";
            IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml + queueXml), null);
            ValidateProject(configuration, "test");
        }

		[Test]
		public void DeserialiseTwoProjectsFromXml()
		{
			string projectXml = ConfigurationFixture.GenerateProjectXml("test");
			string project2Xml = ConfigurationFixture.GenerateProjectXml("test2");
			IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml + project2Xml), null);
			ValidateProject(configuration, "test");
			ValidateProject(configuration, "test2");
		}

		[Test] // [CCNET-63] XML comments before project tag was causing NetReflectorException
		public void DeserialiseSingleProjectFromXmlWithComments()
		{
			string projectXml = @"<!-- A Comment -->" + ConfigurationFixture.GenerateProjectXml("test");
			IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml), null);
			ValidateProject(configuration, "test");
		}

		[Test]
		public void DeserialiseCustomProjectFromXml()
		{
			string xml = @"<customtestproject name=""foo"" />";
			IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(xml), null);
			Assert.IsNotNull(configuration.Projects["foo"]);
			Assert.IsTrue(configuration.Projects["foo"] is CustomTestProject);
			Assert.AreEqual("foo", ((CustomTestProject) configuration.Projects["foo"]).Name);
		}

		[Test]
        [ExpectedException(typeof(ConfigurationException))]
		public void DeserialiseProjectFromXmlWithUnusedNodesShouldGenerateEvent()
		{
			string xml = @"<customtestproject name=""foo"" bar=""baz"" />";
			IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(xml), null);
		}

		[Test, ExpectedException(typeof(ConfigurationException))]
		public void AttemptToDeserialiseProjectWithMissingXmlForRequiredProperties()
		{
			string projectXml = @"<project />";
			reader.Read(ConfigurationFixture.GenerateConfig(projectXml), null);
		}

		[Test, ExpectedException(typeof(ConfigurationException))]
		public void AttemptToDeserialiseProjectFromEmptyDocument()
		{
			reader.Read(new XmlDocument(), null);
		}

		[Test, ExpectedException(typeof(ConfigurationException))]
		public void AttemptToDeserialiseProjectFromXmlWithInvalidRootElement()
		{
			reader.Read(XmlUtil.CreateDocument("<loader/>"), null);
		}

        [Test]
        public void QueueValidationForQueueWithProjectName()
        {
            string projectXml = ConfigurationFixture.GenerateProjectXml("test");
            string queueXml = "<queue name=\"test\" duplicates=\"ApplyForceBuildsReAdd\"/>";
            IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml + queueXml), null);
            ValidateProject(configuration, "test");
        }

        [Test]
        public void QueueValidationForQueueWithQueueNameInProject()
        {
            string projectXml = ConfigurationFixture.GenerateProjectXml("test", "testQueue");
            string queueXml = "<queue name=\"testQueue\" duplicates=\"ApplyForceBuildsReAdd\"/>";
            IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml + queueXml), null);
            ValidateProject(configuration, "test");
        }

        [Test]
        [ExpectedException(typeof(ConfigurationException), ExpectedMessage="An unused queue definition has been found: name 'testQueue'")]
        public void QueueValidationForUnreferencedQueue()
        {
            string projectXml = ConfigurationFixture.GenerateProjectXml("test");
            string queueXml = "<queue name=\"testQueue\" duplicates=\"ApplyForceBuildsReAdd\"/>";
            IConfiguration configuration = reader.Read(ConfigurationFixture.GenerateConfig(projectXml + queueXml), null);
            ValidateProject(configuration, "test");
        }
        
		private void ValidateProject(IConfiguration configuration, string projectName)
		{
			Project project = configuration.Projects[projectName] as Project;
			Assert.AreEqual(projectName, project.Name);
			Assert.IsTrue(project.Tasks[0] is NullTask);
			Assert.IsTrue(project.SourceControl is NullSourceControl);
			Assert.AreEqual(1, project.Publishers.Length);
			Assert.IsTrue(project.Publishers[0] is NullTask);
		}

        [ReflectorType("garbage")]
        class Garbage
        {
        }

		[ReflectorType("customtestproject")]
		class CustomTestProject : ProjectBase, IProject
		{
			public IIntegrationResult Integrate(IntegrationRequest request) { return null; }
			public void NotifyPendingState() {}
			public void NotifySleepingState() {}
			public void Purge(bool purgeWorkingDirectory, bool purgeArtifactDirectory, bool purgeSourceControlEnvironment) { }

			public string Statistics
			{
				get { throw new NotImplementedException(); }
			}

            #region Links
            /// <summary>
            /// Link this project to other sites.
            /// </summary>
            public NameValuePair[] LinkedSites { get; set; }
            #endregion

            public string ModificationHistory
            {
                get { throw new NotImplementedException(); }
            }

            public string RSSFeed
            {
                get { throw new NotImplementedException(); }
            }


			public IIntegrationRepository IntegrationRepository
			{
				get { throw new NotImplementedException(); }
			}

			public string QueueName
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public int QueuePriority
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public void Initialize() {}

			public ProjectStatus CreateProjectStatus(IProjectIntegrator integrator)
			{
				throw new NotImplementedException();
			}

            public ProjectActivity CurrentActivity
            {
                get { throw new NotImplementedException(); }
            }
			
			public void AbortRunningBuild()
			{
				throw new NotImplementedException();
			}
			
			public void AddMessage(Message message)
			{
				throw new NotImplementedException();
			}

			public string WebURL { get {return string.Empty; } }
            public IProjectAuthorisation Security
            {
                get { return null; }
            }

            public int MaxSourceControlRetries { get { return 0; } }

            public ProjectInitialState InitialState { get { return ProjectInitialState.Started; } }
            public ProjectStartupMode StartupMode { get { return ProjectStartupMode.UseInitialState; } }

            #region IProject Members


            public bool stopProjectOnReachingMaxSourceControlRetries
            {
                get { throw new NotImplementedException(); }
            }

            public Common.SourceControlErrorHandlingPolicy SourceControlErrorHandling
            {
                get { throw new NotImplementedException(); }
            }

            #endregion

            #region RetrievePackageList()
            /// <summary>
            /// Retrieves the latest list of packages.
            /// </summary>
            /// <returns></returns>
            public virtual List<PackageDetails> RetrievePackageList()
            {
                List<PackageDetails> packages = new List<PackageDetails>();
                return packages;
            }

            /// <summary>
            /// Retrieves the list of packages for a build.
            /// </summary>
            /// <param name="buildLabel"></param>
            /// <returns></returns>
            public virtual List<PackageDetails> RetrievePackageList(string buildLabel)
            {
                List<PackageDetails> packages = new List<PackageDetails>();
                return packages;
            }
            #endregion
        }
	}
}
