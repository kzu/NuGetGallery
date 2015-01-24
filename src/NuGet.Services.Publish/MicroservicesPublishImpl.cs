﻿using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.Services.Publish
{
    public class MicroservicesPublishImpl : PublishImpl
    {
        static ISet<string> Files = new HashSet<string> { "microservice.json" };

        public MicroservicesPublishImpl(IRegistrationOwnership registrationOwnership)
            : base(registrationOwnership)
        {
        }

        protected override bool IsMetadataFile(string fullName)
        {
            return Files.Contains(fullName);
        }

        protected override JObject CreateMetadataObject(string fullname, Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            JObject obj = JObject.Parse(reader.ReadToEnd());

            string id = obj["id"].ToString();
            string version = NuGetVersion.Parse(obj["version"].ToString()).ToNormalizedString();

            Uri jsonLdId = new Uri("http://" + id + "/" + version);

            obj["@id"] = jsonLdId.ToString();
            obj["@context"] = new JObject { { "@vocab", "http://schema.nuget.org/schema#" } };

            return obj;
        }

        protected override void GenerateNuspec(IDictionary<string, JObject> metadata)
        {
            JObject microservice = metadata["microservice.json"];

            JObject nuspec = new JObject
            {
                { "@id", microservice["@id"] },
                { "id", microservice["id"] },
                { "version", microservice["version"] },
                { "@context", microservice["@context"] }
            };

            metadata["nuspec.json"] = nuspec;
        }

        protected override string Validate(IDictionary<string, JObject> metadata, Stream nupkgStream)
        {
            if (metadata.Count == 0)
            {
                return "no metadata was found in the package";
            }

            JObject nuspec;
            if (!metadata.TryGetValue("microservice.json", out nuspec))
            {
                return "microservice.json was found in the package";
            }

            JToken id;
            if (!nuspec.TryGetValue("id", out id))
            {
                return "required property 'id' was missing from metadata";
            }

            JToken version;
            if (!nuspec.TryGetValue("version", out version))
            {
                return "required property 'version' was missing from metadata";
            }

            return null;
        }
    }
}