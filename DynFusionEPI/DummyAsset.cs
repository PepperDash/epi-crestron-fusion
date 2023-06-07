using PepperDash.Core;

namespace DynFusion
{
    public class DummyAsset : IKeyed
    {
        public string Key { get; private set; }
        public DummyAsset(string key)
        {
            Key = key;
        }
    }
}