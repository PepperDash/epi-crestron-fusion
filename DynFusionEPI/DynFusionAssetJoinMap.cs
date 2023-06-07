using System.Collections.Generic;
using Crestron.SimplSharpPro;
using PepperDash.Essentials.Core;

namespace DynFusion
{
    public class DynFusionStaticAssetJoinMapDynamic : JoinMapBaseAdvanced
    {
        public DynFusionStaticAssetJoinMapDynamic(uint joinStart, IEnumerable<DynFusionAttributeBase> baseAttributes,
            IEnumerable<DynFusionAttributeBase> customAttributes)
            : base(joinStart, typeof(DynFusionStaticAssetJoinMapDynamic))
        {
            foreach (var dynFusionAttributeBase in baseAttributes)
            {
                var attribute = dynFusionAttributeBase;
                BuildBaseAttributeJoinData(attribute, joinStart);
            }
            foreach (var dynFusionAttributeBase in customAttributes)
            {
                var attribute = dynFusionAttributeBase;
                BuildCustomAttributeJoinData(attribute, joinStart);
            }
        }

        private void BuildBaseAttributeJoinData(DynFusionAttributeBase attribute, uint joinStart)
        {
            var joinCapabilities = (eJoinCapabilities)attribute.RwType;

            var joinType = eJoinType.None;
            switch (attribute.SignalType)
            {
                case eSigType.Bool:
                    joinType = eJoinType.Digital;
                    break;
                case eSigType.UShort:
                    joinType = eJoinType.Analog;
                    break;
                case eSigType.String:
                    joinType = eJoinType.Serial;
                    break;
            }

            var joinData = new JoinData
            {
                JoinNumber = attribute.JoinNumber + joinStart - 1,
                JoinSpan = 1
            };

            var joinMetaData = new JoinMetadata
            {
                Description = string.Format("Interact with Fusion Asset Base Attribute [ {0} ]", attribute.Name),
                JoinCapabilities = joinCapabilities,
                JoinType = joinType
            };

            var joinDataComplete = new JoinDataComplete(joinData, joinMetaData);
            Joins.Add(attribute.Name, joinDataComplete);

        }

        private void BuildCustomAttributeJoinData(DynFusionAttributeBase attribute, uint joinStart)
        {
            var joinCapabilities = (eJoinCapabilities)attribute.RwType;

            var joinType = eJoinType.None;
            switch (attribute.SignalType)
            {
                case eSigType.Bool:
                    joinType = eJoinType.Digital;
                    break;
                case eSigType.UShort:
                    joinType = eJoinType.Analog;
                    break;
                case eSigType.String:
                    joinType = eJoinType.Serial;
                    break;
            }

            var joinData = new JoinData
            {
                JoinNumber = attribute.JoinNumber + joinStart - 1 + 10,
                JoinSpan = 1
            };

            var joinMetaData = new JoinMetadata
            {
                Description = string.Format("Interact with Fusion Asset Custom Attribute [ {0} ] at Fusion Asset Join [ {1} ]", attribute.Name, attribute.JoinNumber + 49),
                JoinCapabilities = joinCapabilities,
                JoinType = joinType
            };

            var joinDataComplete = new JoinDataComplete(joinData, joinMetaData);
            Joins.Add(attribute.Name, joinDataComplete);

        }

    }
}