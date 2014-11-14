﻿using System;
using System.Collections.Generic;
using AppGet.Manifests;
using AppGet.Serialization;
using NUnit.Framework;

namespace AppGet.Tests.Manifests
{
    [TestFixture]
    public class PackageManifestFixture
    {
        [Test]
        public void print_sample_manifest()
        {
            var manifest = new PackageManifest
            {
                Id = "linqpad",
                Version = "4.51.03",
                Exe = new[] { "LINQPad.exe" },
                ProductUrl ="http://www.linqpad.net/",
                InstallMethod = InstallMethodType.Zip,
                Installers = new List<Installer>
                {
                    new Installer
                    {
                        Location = "http://www.linqpad.net/GetFile.aspx?LINQPad4-AnyCPU.zip",
                        Architecture = ArchitectureType.Any
                    }
                }
            };


            Console.WriteLine(Yaml.Serialize(manifest));
        }
    }
}