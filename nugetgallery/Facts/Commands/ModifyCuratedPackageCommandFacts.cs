﻿using System;
using Moq;
using Xunit;

namespace NuGetGallery.Commands
{
    public class ModifyCuratedPackageCommandFacts
    {
        public class TheExecuteMethod
        {
            [Fact]
            public void WillThrowWhenCuratedFeedDoesNotExist()
            {
                var cmd = new TestableModifyCuratedPackageCommand();
                cmd.StubCuratedFeedByKeyQry
                    .Setup(stub => stub.Execute(It.IsAny<int>(), It.IsAny<bool>()))
                    .Returns((CuratedFeed)null);

                Assert.Throws<InvalidOperationException>(() => cmd.Execute(
                    42,
                    0,
                    false));
            }

            [Fact]
            public void WillThrowWhenCuratedPackageDoesNotExist()
            {
                var cmd = new TestableModifyCuratedPackageCommand();
                cmd.StubCuratedFeed.Packages = new[] {new CuratedPackage() {Key = 0}};

                Assert.Throws<InvalidOperationException>(() => cmd.Execute(
                    0,
                    1066,
                    false));
            }

            [Fact]
            public void WillModifyAndSaveTheCuratedPackage()
            {
                var cmd = new TestableModifyCuratedPackageCommand();
                var stubCuratedPackage = new CuratedPackage() {Key = 1066};
                cmd.StubCuratedFeed.Packages = new[] { stubCuratedPackage };

                cmd.Execute(
                    0,
                    1066,
                    true);

                Assert.True(stubCuratedPackage.Included);
                cmd.StubEntitiesContext.Verify(stub => stub.SaveChanges());
            }
        }
        
        public class TestableModifyCuratedPackageCommand : ModifyCuratedPackageCommand
        {
            public TestableModifyCuratedPackageCommand()
                : base(null)
            {
                StubCuratedFeed = new CuratedFeed { Key = 0, Name = "aName", };
                StubCuratedFeedByKeyQry = new Mock<ICuratedFeedByKeyQuery>();
                StubEntitiesContext = new Mock<IEntitiesContext>();

                StubCuratedFeedByKeyQry
                   .Setup(stub => stub.Execute(It.IsAny<int>(), It.IsAny<bool>()))
                   .Returns(StubCuratedFeed);

                Entities = StubEntitiesContext.Object;
            }

            public CuratedFeed StubCuratedFeed { get; set; }
            public Mock<ICuratedFeedByKeyQuery> StubCuratedFeedByKeyQry { get; set; }
            public Mock<IEntitiesContext> StubEntitiesContext { get; private set; }

            protected override T GetService<T>()
            {
                if (typeof(T) == typeof(ICuratedFeedByKeyQuery))
                    return (T)StubCuratedFeedByKeyQry.Object;

                throw new Exception("Tried to get unexpected service");
            }
        }
    }
}
