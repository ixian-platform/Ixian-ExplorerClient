using IxianExplorerClient.Meta;
using IXICore;
using System;
using System.Collections.Generic;
using System.Net;


namespace IxianExplorerClient.API
{
    class APIServer : GenericAPIServer
    {
        public APIServer()
        {
        }     

        protected override bool processRequest(HttpListenerContext context, string methodName, Dictionary<string, object> parameters)
        {
            JsonResponse? response = null;

            // Override default activity endpoint
            if (!Config.enableActivityScanner && methodName.Equals("activity", StringComparison.OrdinalIgnoreCase))
            {
                response = onActivity(parameters);
            }

            if (methodName.Equals("scan", StringComparison.OrdinalIgnoreCase))
            {
                response = onScan(parameters);
            }

            if (methodName.Equals("resources", StringComparison.OrdinalIgnoreCase))
            {
                onResources(context);
                context.Response.Close();
                return true;
            }

            // Check for default endpoints
            if (response == null)
            {
                response = processDefaultRequest(context, methodName, parameters);
            }

            // Forward calls to the explorer API
            if (response == null)
            {
                try
                {
                    string? targetUrl = APIClient.mapUrlToExplorerAPI(context.Request.Url);
                    if (string.IsNullOrEmpty(targetUrl))
                    {
                        response = new JsonResponse();
                        response.error = new JsonError
                        {
                            code = 404,
                            message = "Endpoint not found"
                        };
                        context.Response.StatusCode = 404;
                    }
                    else
                    {
                        response = APIClient.forwardRequest(context.Request, targetUrl).GetAwaiter().GetResult();
                        context.Response.StatusCode = response.error == null ? 200 : 500;
                    }
                }
                catch (Exception ex)
                {
                    response!.error = new JsonError
                    {
                        code = 500,
                        message = $"Internal Server Error: {ex.Message}"
                    };
                    context.Response.StatusCode = 500;
                }
            }

            context.Response.ContentType = "application/json";
            sendResponse(context.Response, response);

            context.Response.Close();

            return true;
        }


        private JsonResponse onActivity(Dictionary<string, object> parameters)
        {
            return new JsonResponse { result = null, error = new JsonError() { code = (int)RPCErrorCode.RPC_INTERNAL_ERROR, message = "Activity not enabled" } };
        }

        private JsonResponse onScan(Dictionary<string, object> parameters)
        {
            ActivityScanner.clearStorage();
            ActivityScanner.fetchAll();
            return new JsonResponse { result = null, error = null };
        }

    }
}
