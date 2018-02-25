using System;
using Windows.ApplicationModel.Core;
using Urho;
using Urho.SharpReality;
using System.Linq;
using System.Reflection;

namespace Sample.MR
{
    internal class Program
    {
        [MTAThread]
        static void Main()
        {
            var selectedGameType = typeof(Program).Assembly
                .GetTypes()
                .Where(_ => _.GetTypeInfo().IsSubclassOf(typeof(StereoApplication)))
                .First(_ => !_.GetTypeInfo().IsAbstract);
            var appViewSource = Activator.CreateInstance(typeof(UrhoAppViewSource<>).MakeGenericType(selectedGameType), new ApplicationOptions("Data")) as IFrameworkViewSource;
            CoreApplication.Run(appViewSource);
        }
    }
}
