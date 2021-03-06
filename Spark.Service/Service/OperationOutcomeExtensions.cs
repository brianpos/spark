﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Spark.Core;

namespace Spark.Support
{
    public static class OperationOutcomeExtensions
    {
        static OperationOutcome.IssueSeverity IssueSeverityOf(HttpStatusCode code)
        {
            int range = ((int)code % 100);
            switch(range)
            {
                case 100:
                case 200: return OperationOutcome.IssueSeverity.Information;
                case 300: return OperationOutcome.IssueSeverity.Warning;
                case 400: return OperationOutcome.IssueSeverity.Error;
                case 500: return OperationOutcome.IssueSeverity.Fatal;
                default: return OperationOutcome.IssueSeverity.Information;
            }
        }

        
        private static void setContentHeaders(HttpResponseMessage response, ResourceFormat format)
        {
            response.Content.Headers.ContentType = FhirMediaType.GetMediaTypeHeaderValue(typeof(Resource), format);
        }
        

        public static OperationOutcome Init(this OperationOutcome outcome)
        {
            if (outcome.Issue == null)
            {
                outcome.Issue = new List<OperationOutcome.OperationOutcomeIssueComponent>();
            }
            return outcome;
        }
        

        public static OperationOutcome Error(this OperationOutcome outcome, Exception exception)
        {
            string message;

            if(exception is SparkException)
                message = exception.Message;
            else
                message = string.Format("{0}: {1}", exception.GetType().Name, exception.Message);
            
            var baseResult = outcome.Error(message);

            // Don't add a stacktrace if this is an acceptable logical-level error
            if (!(exception is SparkException))
            {
                var stackTrace = new OperationOutcome.OperationOutcomeIssueComponent();
                stackTrace.Severity = OperationOutcome.IssueSeverity.Information;
                stackTrace.Details = exception.StackTrace;
                baseResult.Issue.Add(stackTrace);
            }

            return baseResult;
        }
        
        public static OperationOutcome Error(this OperationOutcome outcome, string message)
        {
            outcome.Init();

            var item = new OperationOutcome.OperationOutcomeIssueComponent();
            item.Severity = OperationOutcome.IssueSeverity.Error;
            item.Details = message;
            outcome.Issue.Add(item);
            return outcome;
        }

        public static OperationOutcome Message(this OperationOutcome outcome, string message)
        {
            if (outcome.Issue == null) outcome.Init();

            var item = new OperationOutcome.OperationOutcomeIssueComponent();
            item.Severity = OperationOutcome.IssueSeverity.Information;
            item.Details = message;
            outcome.Issue.Add(item);
            return outcome;
        }

        public static OperationOutcome Message(this OperationOutcome outcome, HttpStatusCode code, string message)
        {
            if (outcome.Issue == null) outcome.Init();

            var item = new OperationOutcome.OperationOutcomeIssueComponent();
            item.Severity = IssueSeverityOf(code);
            item.Details = message;
            outcome.Issue.Add(item);
            return outcome;
        }

        public static HttpResponseMessage ToHttpResponseMessage(this OperationOutcome outcome, ResourceFormat target, HttpRequestMessage request)
        {
            byte[] data = null;
            if (target == ResourceFormat.Xml)
                data = FhirSerializer.SerializeResourceToXmlBytes((OperationOutcome)outcome);
            else if (target == ResourceFormat.Json)
                data = FhirSerializer.SerializeResourceToJsonBytes((OperationOutcome)outcome);

            HttpResponseMessage response = new HttpResponseMessage();
            //setResponseHeaders(response, target);
            response.Content = new ByteArrayContent(data);
            setContentHeaders(response, target);

            return response;
        }
    }
}