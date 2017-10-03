//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Extensibility;

namespace Microsoft.SqlTools.Azure.Core.Extensibility
{
    /// <summary>
    /// Enables tracing feature for classes
    /// </summary>
    internal class Traceable : TraceableBase, IComposableService
    {
        private IMultiServiceProvider _serviceProvider;
        private ITrace _trace;

        public Traceable()
        {
        }

        public Traceable(ITrace trace)
        {
            _trace = trace;
        }

        public override ITrace Trace
        {
            get
            {
                if (_trace == null)
                {
                    if (_serviceProvider != null)
                    {
                        _trace = _serviceProvider.GetService<ITrace>();
                    }
                }
                return _trace;
            }
            set
            {
                _trace = value;
            }
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            _serviceProvider = provider;
        }
    }
}
