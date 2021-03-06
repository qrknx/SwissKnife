using BlazorJsBindingsGenerator;

namespace SwissKnife.Serverless;

[JsBind("localStorage.getItem", Params = typeof((string key, int)), ReturnsNullable = typeof(string))]
[JsBind("localStorage.setItem", Params = typeof((string key, string value)))]
[JsBind("alert", Params = typeof((string message, int)))]
[JsBind("prompt", Params = typeof((string message, int)), ReturnsNullable = typeof(string))]
[JsBindingContext(JsPrefix = "BlazorBindings")]
public static partial class JsBindings
{

}
