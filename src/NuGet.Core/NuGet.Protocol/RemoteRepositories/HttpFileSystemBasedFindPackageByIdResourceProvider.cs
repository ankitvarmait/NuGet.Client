﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol
{
    public class HttpFileSystemBasedFindPackageByIdResourceProvider : ResourceProvider
    {
        public HttpFileSystemBasedFindPackageByIdResourceProvider()
            : base(typeof(FindPackageByIdResource),
                nameof(HttpFileSystemBasedFindPackageByIdResourceProvider),
                before: nameof(RemoteV3FindPackageByIdResourceProvider))
        {
        }

        public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository sourceRepository, CancellationToken token)
        {
            INuGetResource resource = null;
            var serviceIndexResource = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            var packageBaseAddress = serviceIndexResource?.GetServiceEntryUris(ServiceTypes.PackageBaseAddress);

            if (packageBaseAddress != null
                && packageBaseAddress.Count > 0)
            {
                var httpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);
                var idListResource = await sourceRepository.GetResourceAsync<IdListResource>(token);

                resource = new HttpFileSystemBasedFindPackageByIdResource(
                    sourceRepository,
                    packageBaseAddress,
                    httpSourceResource.HttpSource,
                    idListResource);
            }

            return Tuple.Create(resource != null, resource);
        }
    }
}
