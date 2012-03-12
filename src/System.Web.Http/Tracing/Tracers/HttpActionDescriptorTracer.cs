﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web.Http.Common;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Properties;

namespace System.Web.Http.Tracing.Tracers
{
    /// <summary>
    /// Tracer for <see cref="HttpActionDescriptor"/>.
    /// </summary>
    internal class HttpActionDescriptorTracer : HttpActionDescriptor
    {
        private const string ExecuteMethodName = "Execute";

        private readonly HttpActionDescriptor _innerDescriptor;
        private readonly ITraceWriter _traceWriter;

        public HttpActionDescriptorTracer(HttpControllerContext controllerContext, HttpActionDescriptor innerDescriptor, ITraceWriter traceWriter) : base(controllerContext.ControllerDescriptor)
        {
            _innerDescriptor = innerDescriptor;
            _traceWriter = traceWriter;
        }

        public override string ActionName
        {
            get { return _innerDescriptor.ActionName; }
        }

        public override Type ReturnType
        {
            get { return _innerDescriptor.ReturnType; }
        }

        public override object Execute(HttpControllerContext controllerContext, IDictionary<string, object> arguments)
        {
            object result = null;

            _traceWriter.TraceBeginEnd(
                controllerContext.Request,
                TraceCategories.ActionCategory,
                TraceLevel.Info,
                _innerDescriptor.GetType().Name,
                ExecuteMethodName,
                beginTrace: (tr) =>
                {
                    tr.Message = Error.Format(SRResources.TraceInvokingAction,
                                              FormattingUtilities.ActionInvokeToString(this.ActionName, arguments));
                },
                execute: () =>
                {
                    result = _innerDescriptor.Execute(controllerContext, arguments);
                },
                endTrace: (tr) => 
                {
                    tr.Message = Error.Format(SRResources.TraceActionReturnValue,
                                              FormattingUtilities.ValueToString(result, CultureInfo.CurrentCulture));
                },
                errorTrace: null);

            return result;
        }

        public override IEnumerable<T> GetCustomAttributes<T>()
        {
            return _innerDescriptor.GetCustomAttributes<T>();
        }

        public override IEnumerable<IFilter> GetFilters()
        {
            List<IFilter> filters = new List<IFilter>(_innerDescriptor.GetFilters());
            List<IFilter> returnFilters = new List<IFilter>(filters.Count);
            for (int i = 0; i < filters.Count; i++)
            {
                if (FilterTracer.IsFilterTracer(filters[i]))
                {
                    returnFilters.Add(filters[i]);
                }
                else
                {
                    IEnumerable<IFilter> filterTracers = FilterTracer.CreateFilterTracers(filters[i], _traceWriter);
                    foreach (IFilter filterTracer in filterTracers)
                    {
                        returnFilters.Add(filterTracer);
                    }
                }
            }

            return returnFilters;
        }

        public override Collection<FilterInfo> GetFilterPipeline()
        {
            List<FilterInfo> filters = new List<FilterInfo>(_innerDescriptor.GetFilterPipeline());
            List<FilterInfo> returnFilters = new List<FilterInfo>(filters.Count);
            for (int i = 0; i < filters.Count; i++)
            {
                // If this filter has been wrapped already, use as is
                if (FilterTracer.IsFilterTracer(filters[i].Instance))
                {
                    returnFilters.Add(filters[i]);
                }
                else
                {
                    IEnumerable<FilterInfo> filterTracers = FilterTracer.CreateFilterTracers(filters[i], _traceWriter);
                    foreach (FilterInfo filterTracer in filterTracers)
                    {
                        returnFilters.Add(filterTracer);
                    }
                }
            }

            return new Collection<FilterInfo>(returnFilters);
        }

        public override Collection<HttpParameterDescriptor> GetParameters()
        {
            return _innerDescriptor.GetParameters();
        }
    }
}