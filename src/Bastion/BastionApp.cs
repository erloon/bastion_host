using Amazon.CDK;
using Constructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bastion
{
    public class BastionApp : Stage
    {
        public BastionApp(Construct scope, string id, IStageProps props = null) : base(scope, id, props)
        {
            var networkStack = new NetworkStack(scope, "network-stack", new StackProps
            {
                StackName = "network-stack"
            });

            _ = new BastionStack(scope, "bastion-host-stack", networkStack.Vpc, new StackProps
            {
                StackName = "bastion-host-stack"
            });
        }
    }
}
