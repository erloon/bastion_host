using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using System.Text.Json;
using Secret = Amazon.CDK.AWS.SecretsManager.Secret;
using RdsCredentials = Amazon.CDK.AWS.RDS.Credentials;

namespace Bastion
{
    public class BastionStack : Stack
    {
        internal BastionStack(Construct scope, string id, IVpc vpc, IStackProps props = null) : base(scope, id, props)
        {
            var bastionSecurityGroup = new SecurityGroup(this, "bastion-security-group", new SecurityGroupProps
            {
                Vpc = vpc,
                AllowAllOutbound = true,
                SecurityGroupName="bastion-security-group",
                Description = "security group for bastion host"
            });

            bastionSecurityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(22), "SSH access");

            var bastion = new BastionHostLinux(this, "bastion-host", new BastionHostLinuxProps
            {
                Vpc = vpc,
                InstanceName = "bastion-hos",
                SecurityGroup = bastionSecurityGroup,
                InstanceType = new InstanceType("t2.micro"),
                SubnetSelection = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                }
            });

            var rdsSecurityGroup = new SecurityGroup(this, "rds-security-group", new SecurityGroupProps
            {
                Vpc = vpc,
                SecurityGroupName = "rds-security-group",
                Description = "Security group for accessing Aurora",
                AllowAllOutbound = false
            });

            var userRoot = new Secret(this, "rds-credentials", new SecretProps()
            {
                RemovalPolicy = Amazon.CDK.RemovalPolicy.DESTROY,
                SecretName = "rds-root",
                GenerateSecretString = new SecretStringGenerator()
                {
                    SecretStringTemplate = JsonSerializer.Serialize(new { username = "dbroot" }),
                    ExcludePunctuation = true,
                    IncludeSpace = false,
                    ExcludeCharacters = "/!@#$%;^&*\\",
                    GenerateStringKey = "password"
                },
            });

            var subnetGroup = new SubnetGroup(this, "subnet-group", new SubnetGroupProps
            {
                Vpc = vpc,
                Description = "subnet group for postgres rds",
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_ISOLATED
                },
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            var rdsCredentials = RdsCredentials.FromSecret(userRoot);
            var rds = new DatabaseCluster(this, "rds", new DatabaseClusterProps
            {
                Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
                {
                    Version = AuroraPostgresEngineVersion.VER_15_2
                }),
                ClusterIdentifier = "my-rds",
                SubnetGroup = subnetGroup,
                Instances = 1,
                InstanceProps = new Amazon.CDK.AWS.RDS.InstanceProps
                {
                    Vpc = vpc,
                    SecurityGroups = new ISecurityGroup[] { rdsSecurityGroup },
                    InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                    VpcSubnets = new SubnetSelection
                    {
                        SubnetType = SubnetType.PRIVATE_ISOLATED
                    }
                },
                DefaultDatabaseName = "mydb",
                Credentials = rdsCredentials,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            rds.Connections.AllowDefaultPortFrom(bastionSecurityGroup, "Allow access from bastion host");
            rds.Connections.AllowTo(bastionSecurityGroup, Port.Tcp(5432), "Allow access from bastion host");
        }
    }
}
