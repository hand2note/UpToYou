using System;
using AutoFixture;
using AutoFixture.NUnit3;

namespace UpToYou.Core.Tests
{
    public class MyAutoDataAttribute : AutoDataAttribute {
        public MyAutoDataAttribute() : base(AutoDataCustomization.Factory) { }
    }

    public static class AutoDataCustomization {

        public static IFixture 
            Factory() {
            var fixture = new Fixture();

            fixture.Register<int, int, int, Version>((x,y,z) => new Version(x,y,z));

            return fixture;
        }
    }

}