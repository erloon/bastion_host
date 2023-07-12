using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bastion
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            _ = new BastionApp(app, "bastion-app", new StageProps()
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = "523397174287",
                    Region = "us-east-1",
                }
            });
            app.Synth();
        }
    }
}
