using System.Reflection;
using Icod.CommandFramework.RegularExpressions;
using Icod.CommandFramework.Text;

// Stage A tooling: list the compiled, externally visible regex declarations.
// Keep this tool independent of source parsing and external API-diff packages.
var assembly = typeof( IRegularExpressionProvider ).Assembly;
var nullable = new NullabilityInfoContext();
var included = assembly.GetExportedTypes()
	.Where( type => type.Namespace == "Icod.CommandFramework.RegularExpressions"
		|| type == typeof( TextDecodingMode )
		|| type == typeof( InvalidEncodingPolicy ) )
	.OrderBy( type => type.FullName, StringComparer.Ordinal )
	.ToArray();

Console.WriteLine( $"# {assembly.GetName().Name} {assembly.GetName().Version} compiled regex API" );
Console.WriteLine( "# Public declared members, including compiler-generated record members; inherited framework members excluded." );
Console.WriteLine( $"# Top-level regex types: {included.Count( type => type.Namespace == "Icod.CommandFramework.RegularExpressions" && !type.IsNested )}" );
Console.WriteLine( "# Referenced external text enums: TextDecodingMode, InvalidEncodingPolicy" );

foreach ( var type in included ) {
	var kind = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct"
		: type.IsAbstract && type.IsSealed ? "static class" : type.IsSealed ? "sealed class"
		: type.IsAbstract ? "abstract class" : "class";
	var ancestry = new List<string>();
	if ( type.BaseType is { } baseType && baseType != typeof( object ) && baseType != typeof( ValueType ) && baseType != typeof( Enum ) ) ancestry.Add( Format( baseType ) );
	ancestry.AddRange( type.GetInterfaces().Select( iface => Format( iface ) ).OrderBy( name => name, StringComparer.Ordinal ) );
	Console.WriteLine( $"{kind} {Format( type )}{( ancestry.Count == 0 ? string.Empty : " : " + string.Join( ", ", ancestry ) )}" );
	if ( type.IsEnum ) {
		foreach ( var field in type.GetFields( BindingFlags.Public | BindingFlags.Static ).OrderBy( field => field.Name, StringComparer.Ordinal ) ) {
			Console.WriteLine( $"  {field.Name} = {Convert.ToInt64( field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture )}" );
		}
		continue;
	}

	var entries = new List<string>();
	foreach ( var constructor in type.GetConstructors( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly ) ) {
		entries.Add( $"ctor({Parameters( constructor.GetParameters() )})" );
	}
	foreach ( var property in type.GetProperties( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly ) ) {
		var access = property.GetAccessors().Where( method => method.IsPublic ).ToArray();
		if ( access.Length == 0 ) continue;
		var getter = access.Any( method => method == property.GetMethod ) ? "get; " : string.Empty;
		var setter = property.SetMethod is { IsPublic: true } set
			? set.ReturnParameter.GetRequiredCustomModifiers().Any( modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit" ) ? "init; " : "set; "
			: string.Empty;
		var index = property.GetIndexParameters();
		var name = index.Length == 0 ? property.Name : $"this[{Parameters( index )}]";
		var storage = access[0].IsStatic ? "static " : string.Empty;
		entries.Add( $"property {storage}{Format( property.PropertyType, nullable.Create( property ) )} {name} {{ {getter}{setter}}}" );
	}
	foreach ( var field in type.GetFields( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly ) ) {
		entries.Add( $"field {( field.IsStatic ? "static " : string.Empty )}{Format( field.FieldType, nullable.Create( field ) )} {field.Name}" );
	}
	foreach ( var method in type.GetMethods( BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly ) ) {
		if ( method.IsSpecialName && ( method.Name.StartsWith( "get_", StringComparison.Ordinal ) || method.Name.StartsWith( "set_", StringComparison.Ordinal ) || method.Name.StartsWith( "add_", StringComparison.Ordinal ) || method.Name.StartsWith( "remove_", StringComparison.Ordinal ) ) ) continue;
		var generic = method.IsGenericMethod ? $"<{string.Join( ", ", method.GetGenericArguments().Select( arg => arg.Name ) )}>" : string.Empty;
		entries.Add( $"method {( method.IsStatic ? "static " : string.Empty )}{Format( method.ReturnType, nullable.Create( method.ReturnParameter ) )} {method.Name}{generic}({Parameters( method.GetParameters() )})" );
	}
	foreach ( var entry in entries.OrderBy( entry => entry, StringComparer.Ordinal ) ) Console.WriteLine( $"  {entry}" );
}

string Parameters( ParameterInfo[] parameters ) => string.Join( ", ", parameters.Select( parameter => {
	var modifier = parameter.IsOut ? "out " : parameter.ParameterType.IsByRef ? parameter.IsIn ? "in " : "ref " : string.Empty;
	var optional = parameter.HasDefaultValue ? $" = {DefaultValue( parameter )}" : string.Empty;
	return $"{modifier}{Format( parameter.ParameterType, nullable.Create( parameter ) )} {parameter.Name}{optional}";
} ) );

static string DefaultValue( ParameterInfo parameter ) {
	if ( parameter.DefaultValue is null ) return parameter.ParameterType.IsValueType ? "default" : "null";
	if ( parameter.DefaultValue is DBNull || parameter.DefaultValue is Missing ) return "default";
	if ( parameter.ParameterType.IsEnum ) return $"{Format( parameter.ParameterType )}.{Enum.GetName( parameter.ParameterType, parameter.DefaultValue )}";
	return parameter.DefaultValue is bool value ? value ? "true" : "false"
		: parameter.DefaultValue is string text ? $"\"{text}\""
		: Convert.ToString( parameter.DefaultValue, System.Globalization.CultureInfo.InvariantCulture ) ?? "default";
}

static string Format( Type type, NullabilityInfo? info = null ) {
	if ( type.IsByRef ) return Format( type.GetElementType()!, info?.ElementType ?? info );
	if ( type.IsArray ) return $"{Format( type.GetElementType()!, info?.ElementType )}[]{Suffix( type, info )}";
	if ( type.IsGenericParameter ) return type.Name + Suffix( type, info );
	if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof( Nullable<> ) )
		return Format( type.GetGenericArguments()[0], info?.GenericTypeArguments.FirstOrDefault() ) + "?";
	var name = type.FullName switch {
		"System.Void" => "void", "System.Boolean" => "bool", "System.Byte" => "byte", "System.Char" => "char",
		"System.Int32" => "int", "System.Int64" => "long", "System.String" => "string", "System.Object" => "object",
		_ => ( type.FullName ?? type.Name ).Replace( '+', '.' )
	};
	if ( type.IsGenericType ) {
		name = name[..name.IndexOf( '`' )];
		name += $"<{string.Join( ", ", type.GetGenericArguments().Select( (arg, i) => Format( arg, info?.GenericTypeArguments.ElementAtOrDefault( i ) ) ) )}>";
	}
	return name + Suffix( type, info );
}

static string Suffix( Type type, NullabilityInfo? info ) => !type.IsValueType && info?.ReadState == NullabilityState.Nullable ? "?" : string.Empty;
