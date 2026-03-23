using FluentlyHttpClient.Middleware;
using Odin.Core.Error;
using Odin.Core.Http;
using System.Net;

namespace Odin.Core.Http
{
	/// <summary>
	/// Odin errors middleware for HTTP client.
	/// </summary>
	public class OdinErrorHttpMiddleware : OdinErrorBaseHttpMiddleware<OdinErrorResponse>
	{
		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public OdinErrorHttpMiddleware(
			FluentHttpMiddlewareDelegate next,
			FluentHttpMiddlewareClientContext context,
			ILoggerFactory loggerFactory
		) : base(
			next,
			context,
			loggerFactory.CreateLogger($"{typeof(OdinErrorHttpMiddleware).Namespace}.{context.Identifier}.OdinError")
		)
		{
		}

		protected override string MapGlobalErrors(OdinHttpMiddlewareErrorContext<OdinErrorResponse> context)
		{
			switch (context.HttpResponse.StatusCode)
			{
				case HttpStatusCode.Unauthorized:
					return OdinErrorCodes.Auth.Unauthorized;
				case HttpStatusCode.NotFound:
					return OdinErrorCodes.NotFound;
				default:
					return null;
			}
		}

		protected override void ExtractApiErrors(OdinHttpMiddlewareErrorContext<OdinErrorResponse> context)
		{
			var apiError = context.ApiErrorResponse;
			var errorResult = context.ErrorResult;

			if (apiError == null) return;
			context.ErrorResult.UpdateFrom(apiError);

			if (apiError.ErrorCode.IsNullOrEmpty())
				errorResult.ErrorCode = OdinErrorCodes.InternalServerError;

			errorResult.AddData("clientErrorDetails", new OdinClientErrorDetails
			{
				StatusCode = context.HttpResponse.StatusCode
			});
		}
	}
}

namespace FluentlyHttpClient
{
	/// <summary>
	/// Odin error http middleware extensions.
	/// </summary>
	public static class OdinErrorHttpMiddlewareExtensions
	{
		/// <summary>
		/// Use Odin errors middleware.
		/// </summary>
		/// <param name="builder">Builder instance</param>
		public static FluentHttpClientBuilder UseOdinErrors(this FluentHttpClientBuilder builder)
			=> builder.UseMiddleware<OdinErrorHttpMiddleware>();
	}
}
