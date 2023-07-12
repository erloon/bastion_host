using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Bastion
{
    public class NetworkStack : Stack
    {
        public IVpc Vpc { get; }

        internal NetworkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            Vpc = new Vpc(this, "vpc", new VpcProps
            {
                IpAddresses = IpAddresses.Cidr("10.0.0.0/16"),
                NatGateways = 1,
                MaxAzs = 2,
                SubnetConfiguration = new[]
               {
                    new SubnetConfiguration
                    {
                        Name = "public",
                        CidrMask = 24,
                        SubnetType = SubnetType.PUBLIC,
                    },
                    new SubnetConfiguration
                    {
                        Name = "private",
                        CidrMask = 24,
                        SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    },
                     new SubnetConfiguration
                    {
                        Name = "isolated",
                        CidrMask = 24,
                        SubnetType = SubnetType.PRIVATE_ISOLATED,
                    }
                },
            });

            Vpc.AddFlowLog("FlowLog");

            var vpcEndpointSecurityGroup = new SecurityGroup(this, "vpc-endpoint-security-group", new SecurityGroupProps
            {
                AllowAllOutbound = true,
                Description = "VPC endpoint security group for Bastion",
                Vpc = Vpc
            });

            vpcEndpointSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(22), "Allow SSH access from Internet to Bastion host");

            Vpc.AddInterfaceEndpoint("ssm-messages", new InterfaceVpcEndpointProps
            {
                Open = true,
                PrivateDnsEnabled = true,
                Service = InterfaceVpcEndpointAwsService.SSM_MESSAGES,
                Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                SecurityGroups = new ISecurityGroup[] { vpcEndpointSecurityGroup }
            });

            Vpc.AddInterfaceEndpoint("ec2-messages", new InterfaceVpcEndpointOptions
            {
                Open = true,
                PrivateDnsEnabled = true,
                Service = InterfaceVpcEndpointAwsService.EC2_MESSAGES,
                Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                SecurityGroups = new ISecurityGroup[] { vpcEndpointSecurityGroup }
            });

            Vpc.AddInterfaceEndpoint("ssm", new InterfaceVpcEndpointOptions
            {
                Open = true,
                PrivateDnsEnabled = true,
                Service = InterfaceVpcEndpointAwsService.SSM,
                Subnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
                SecurityGroups = new ISecurityGroup[] { vpcEndpointSecurityGroup }
            });
        }
    }
}
