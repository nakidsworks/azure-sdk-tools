﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Test.Utilities.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Commands.Utilities.Common;
    using Management.Storage;
    using Management.Storage.Models;
    using Moq;

    /// <summary>
    /// A class used to simulate the behavior of a storage account as far as existing,
    /// creation, and querying are concerned.
    /// </summary>
    public class MockStorageService
    {
        public class StorageAccountData
        {
            private string blobEndpoint;
            private string queueEndpoint;
            private string tableEndpoint;
            private string primaryKey;
            private string secondaryKey;

            private const string defaultPrimaryKey = "MNao3bm7t7B/x+g2/ssh9HnG0mEh1QV5EHpcna8CetYn+TSRoA8/SBoH6B3Ufwtnz3jZLSw9GEUuCTr3VooBWq==";
            private const string defaultSecondaryKey = "secondaryKey";

            public string Name { get; set; }

            public string BlobEndpoint
            {
                get
                {
                    return blobEndpoint ?? "http://awesome.blob.windows.core.net/";
                }
                set { blobEndpoint = value; }
            }

            public string QueueEndpoint
            {
                get { return queueEndpoint ?? "http://awesome.queue.windows.core.net/"; }
                set { queueEndpoint = value; }
            }

            public string TableEndpoint
            {
                get { return tableEndpoint ?? "http://awesome.table.windows.core.net"; }
                set { tableEndpoint = value; }
            }

            public string PrimaryKey
            {
                get { return primaryKey ?? defaultPrimaryKey; }
                set { primaryKey = value; }
            }

            public string SecondaryKey
            {
                get { return secondaryKey ?? defaultSecondaryKey; }
                set { secondaryKey = value; }
            }
        }

        private readonly List<StorageAccountData> accounts = new List<StorageAccountData>();

        public MockStorageService Add(Action<StorageAccountData> dataSetter)
        {
            var account = new StorageAccountData();
            dataSetter(account);
            accounts.Add(account);
            return this;
        }

        public void Clear()
        {
            accounts.Clear();
        }

        public void InitializeMocks(Mock<StorageManagementClient> mock)
        {
            mock.Setup(c => c.StorageAccounts.GetAsync(It.IsAny<string>()))
                .Returns((string serviceName) => CreateGetResponse(serviceName));

            mock.Setup(c => c.StorageAccounts.GetKeysAsync(It.IsAny<string>()))
                .Returns((string serviceName) => CreateGetKeysResponse(serviceName));

            mock.Setup(c => c.StorageAccounts.BeginCreatingAsync(It.IsAny<StorageAccountCreateParameters>()))
                .Callback((StorageAccountCreateParameters createParameters) => AddService(createParameters))
                .Returns(CreateBeginCreatingResponse);
        }

        private Task<StorageServiceGetResponse> CreateGetResponse(string serviceName)
        {
            Task<StorageServiceGetResponse> resultTask;
            var data = accounts.FirstOrDefault(a => a.Name == serviceName);
            if (data != null)
            {
                var storageServiceGetResponse = new StorageServiceGetResponse
                {
                    ServiceName = data.Name,
                    Properties = new StorageServiceProperties
                    {
                        Endpoints =
                        {
                            new Uri(data.BlobEndpoint),
                            new Uri(data.QueueEndpoint),
                            new Uri(data.TableEndpoint)
                        }
                    }
                };
                resultTask = Tasks.FromResult(storageServiceGetResponse);
            }
            else
            {
                resultTask = Tasks.FromException<StorageServiceGetResponse>(Make404Exception());
            }
            return resultTask;
        }

        private Task<StorageAccountGetKeysResponse> CreateGetKeysResponse(string serviceName)
        {
            Task<StorageAccountGetKeysResponse> resultTask;
            var data = accounts.FirstOrDefault(a => a.Name == serviceName);
            if (data != null)
            {
                var response = new StorageAccountGetKeysResponse
                {
                    PrimaryKey = data.PrimaryKey,
                    SecondaryKey = data.SecondaryKey,
                    StatusCode = HttpStatusCode.OK
                };
                resultTask = Tasks.FromResult(response);
            }
            else
            {
                resultTask = Tasks.FromException<StorageAccountGetKeysResponse>(Make404Exception());
            }
            return resultTask;
        }

        private void AddService(StorageAccountCreateParameters createParameters)
        {
            Add(a =>
            {
                a.Name = createParameters.ServiceName;
            });
        }
        private Task<OperationResponse> CreateBeginCreatingResponse()
        {
            return Tasks.FromResult(new OperationResponse
            {
                RequestId = "unused",
                StatusCode = HttpStatusCode.OK
            });
        }

        private CloudException Make404Exception()
        {
            return new CloudException("Not found", null, new HttpResponseMessage(HttpStatusCode.NotFound), "");
        }
    }
}
