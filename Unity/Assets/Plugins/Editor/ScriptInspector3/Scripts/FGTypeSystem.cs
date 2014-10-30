/* SCRIPT INSPECTOR 3
 * version 3.0 Beta 2, August 2014
 * Copyright © 2012-2014, Flipbook Games
 * 
 * Unity's legendary custom inspector for C#, UnityScript and Boo scripts,
 * now transformed into a powerful Script, Shader, and Text Editor!!!
 * 
 * Follow me on http://twitter.com/FlipbookGames
 * Like Flipbook Games on Facebook http://facebook.com/FlipbookGames
 * Join Unity forum discusion http://forum.unity3d.com/threads/138329
 * Contact info@flipbookgames.com for feedback, bug reports, or suggestions.
 * Visit http://flipbookgames.com/ for more info.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using Debug = UnityEngine.Debug;


namespace ScriptInspector
{

public enum SymbolKind : byte
{
	None,
	Error,
	_Keyword,
	_Snippet,
	Namespace,
	Interface,
	Enum,
	Struct,
	Class,
	Delegate,
	Field,
	ConstantField,
	LocalConstant,
	EnumMember,
	Property,
	Event,
	Indexer,
	Method,
	MethodGroup,
	Constructor,
	Destructor,
	Operator,
	Accessor,
	LambdaExpression,
	Parameter,
	CatchParameter,
	Variable,
	ForEachVariable,
	FromClauseVariable,
	TypeParameter,
	BaseTypesList,
	Instance,
	Null,
	Label,
}

[Flags]
public enum Modifiers
{
	None = 0,
	Public = 1 << 0,
	Internal = 1 << 1,
	Protected = 1 << 2,
	Private = 1 << 3,
	Static = 1 << 4,
	New = 1 << 5,
	Sealed = 1 << 6,
	Abstract = 1 << 7,
	ReadOnly = 1 << 8,
	Volatile = 1 << 9,
	Virtual = 1 << 10,
	Override = 1 << 11,
	Extern = 1 << 12,
	Ref = 1 << 13,
	Out = 1 << 14,
	Params = 1 << 15,
	This = 1 << 16,
}

public enum AccessLevel : byte
{
	None = 0,
	Private = 1, // private
	ProtectedAndInternal = 2, // n/a
	ProtectedOrInternal = 4, // protected internal
	Protected, // protected
	Internal, // internal
	Public, // public
}

[Flags]
public enum AccessLevelMask : byte
{
	None = 0,
	Private = 1, // private
	Protected = 2, // protected
	Internal = 4, // internal
	Public = 8, // public

	Any = Private | Protected | Internal | Public,
	NonPublic = Private | Protected | Internal,
}


public class SymbolReference
{
	protected SymbolReference() {}

	public SymbolReference(ParseTree.BaseNode node)
	{
		parseTreeNode = node;
	}

	public SymbolReference(SymbolDefinition definedSymbol)
	{
		_definition = definedSymbol;
	}

	protected ParseTree.BaseNode parseTreeNode;
	protected uint _resolvedVersion;
	protected SymbolDefinition _definition;
	protected bool resolving = false;
	public static bool dontResolveNow = false;
	public virtual SymbolDefinition definition
	{
		get
		{
			if (_definition != null &&
				(parseTreeNode != null && _resolvedVersion != ParseTree.resolverVersion || !_definition.IsValid()))
				_definition = null;
			
			if (_definition == null)
			{
			//	Debug.Log("Dereferencing " + parseTreeNode.Print());
				if (!resolving)
				{
					if (dontResolveNow)
						Debug.LogWarning("Resolving SymbolReference!");
					resolving = true;
					_definition = SymbolDefinition.ResolveNode(parseTreeNode);
					_resolvedVersion = ParseTree.resolverVersion;
					resolving = false;
				}
				else
				{
					//	UnityEngine.Debug.LogWarning("Recursion while resolving node " + parseTreeNode);
					return SymbolDefinition.unknownSymbol;
				}
				//var leaf = parseTreeNode as ParseTree.Leaf;
				//if (leaf != null && leaf.resolvedSymbol != null)
				//{
				//    _definition = leaf.resolvedSymbol;
				//}
				//else
				//{
				//    var node = parseTreeNode as ParseTree.Node;
				//    var scopeNode = node;
				//    while (scopeNode != null && scopeNode.scope == null)
				//        scopeNode = scopeNode.parent;
				//    if (scopeNode != null)
				//    {
				//        _definition = scopeNode.scope.ResolveNode(node);
				//    }
				//}
				if (_definition == null)
				{
				//	Debug.Log("Failed to resolve SymbolReference: " + parseTreeNode);
					_definition = SymbolDefinition.unknownType;
					_resolvedVersion = ParseTree.resolverVersion;
				}
			}
			return _definition;
		}
	}

	public bool IsBefore(ParseTree.Leaf leaf)
	{
		if (parseTreeNode == null)
			return true;
		var lastLeaf = parseTreeNode as ParseTree.Leaf;
		if (lastLeaf == null)
			lastLeaf = ((ParseTree.Node) parseTreeNode).GetLastLeaf();
		return lastLeaf != null && (lastLeaf.line < leaf.line || lastLeaf.line == leaf.line && lastLeaf.tokenIndex < leaf.tokenIndex);
	}

	public override string ToString()
	{
		return parseTreeNode != null ? parseTreeNode.Print() : _definition.GetName();
	}
}


public abstract class Scope
{
	public static int completionAtLine;
	public static int completionAtTokenIndex;
	
	protected ParseTree.Node parseTreeNode;

	public Scope(ParseTree.Node node)
	{
		parseTreeNode = node;
	}

	public Scope _parentScope;
	public Scope parentScope {
		get {
			if (_parentScope != null || parseTreeNode == null)
				return _parentScope;
			for (var node = parseTreeNode.parent; node != null; node = node.parent)
				if (node.scope != null)
					return node.scope;
			return null;
		}
		set { _parentScope = value; }
	}
	
	public AssemblyDefinition GetAssembly()
	{
		for (Scope scope = this; scope != null; scope = scope.parentScope)
		{
			var cuScope = scope as CompilationUnitScope;
			if (cuScope != null)
				return cuScope.assembly;
		}
		throw new Exception("No Assembly for scope???");
	}

	public abstract SymbolDefinition AddDeclaration(SymbolDeclaration symbol);

	public abstract void RemoveDeclaration(SymbolDeclaration symbol);

	//public virtual SymbolDefinition AddDeclaration(SymbolKind symbolKind, ParseTree.Node definitionNode)
	//{
	//    var symbol = new SymbolDeclaration { scope = this, kind = symbolKind, parseTreeNode = definitionNode };
	//    var definition = AddDeclaration(symbol);
	//    return definition;
	//

	public virtual string CreateAnonymousName()
	{
		return parentScope != null ? parentScope.CreateAnonymousName() : null;
	}

	public virtual void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		leaf.resolvedSymbol = null;
		if (parentScope != null)
			parentScope.Resolve(leaf, numTypeArgs);
	}

	public virtual void ResolveAttribute(ParseTree.Leaf leaf)
	{
		leaf.resolvedSymbol = null;
		if (parentScope != null)
			parentScope.ResolveAttribute(leaf);
	}

	//public SymbolDefinition ResolveNode(ParseTree.BaseNode node)
	//{
	//    var leaf = node as ParseTree.Leaf;
	//    if (leaf != null)
	//    {
	//        Resolve(leaf);
	//        return leaf.resolvedSymbol;
	//    }
	//    if (parentScope != null)
	//        return parentScope.ResolveNode(node);
	//    return null;
	//}

	//class BuiltInTypeDefinition : ReflectedType
	//{
	//    public string aliasName;

	//    public BuiltInTypeDefinition(Type type) : base(type) {}
	//    public override string GetName()
	//    {
	//        return aliasName;
	//    }
	//}

	//public static SymbolDefinition ImportReflectedType(Assembly assembly, string name)
	//{
	//    var t = assembly.GetType(name);
	//    //if (t == null)
	//    //    t = assembly.GetType("UnityEngine." + name);
	//    if (t != null)
	//    {
	//        Debug.Log(name + " => " + t);
	//        var imported = new ReflectedType(t);
	//        return imported;
	//    }
	//    return SymbolDefinition.unknownType;
	//}

	//public abstract SymbolDefinition ImportReflectedType(Type type);
	//{
	//    throw new InvalidOperationException();
	//    //var imported = new ReflectedType(t);
	//    //return imported;
	//}

	public abstract SymbolDefinition FindName(string symbolName, int numTypeParameters);
	//{
	//    return null;
	//}

	public virtual void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		if (parentScope != null)
			parentScope.GetCompletionData(data, fromInstance, assembly);
	}

	public abstract void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly);

	public virtual TypeDefinition EnclosingType()
	{
		return null;
	}

	public virtual void GetExtensionMethodsCompletionData(TypeDefinitionBase forType, Dictionary<string, SymbolDefinition> data)
	{
//	Debug.Log("Extensions for " + forType.GetTooltipText());
 		if (parentScope != null)
			GetExtensionMethodsCompletionData(forType, data);
	}

	public virtual IEnumerable<NamespaceDefinition> VisibleNamespacesInScope()
	{
		if (parentScope != null)
			foreach (var ns in parentScope.VisibleNamespacesInScope())
				yield return ns;
	}
}


public class ReflectedMember : InstanceDefinition
{
	private readonly MemberInfo memberInfo;

	public ReflectedMember(MemberInfo info, SymbolDefinition memberOf)
	{
		switch (info.MemberType)
		{
			case MemberTypes.Constructor:
			case MemberTypes.Method:
				throw new InvalidOperationException();

			case MemberTypes.Field:
				var fieldInfo = (FieldInfo) info;
				modifiers =
					fieldInfo.IsPublic ? Modifiers.Public :
					fieldInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
					fieldInfo.IsAssembly ? Modifiers.Internal :
					fieldInfo.IsFamily ? Modifiers.Protected :
					Modifiers.Private;
				if (fieldInfo.IsStatic)// && !fieldInfo.IsLiteral)
					modifiers |= Modifiers.Static;
				break;

			case MemberTypes.Property:
				var propertyInfo = (PropertyInfo) info;
				var getMethodInfo = propertyInfo.GetGetMethod(true);
				var setMethodInfo = propertyInfo.GetSetMethod(true);
				modifiers =
					(getMethodInfo != null && getMethodInfo.IsPublic) || (setMethodInfo != null && setMethodInfo.IsPublic) ? Modifiers.Public :
					(getMethodInfo != null && getMethodInfo.IsFamilyOrAssembly) || (setMethodInfo != null && setMethodInfo.IsFamilyOrAssembly) ? Modifiers.Internal | Modifiers.Protected :
					(getMethodInfo != null && getMethodInfo.IsAssembly) || (setMethodInfo != null && setMethodInfo.IsAssembly) ? Modifiers.Internal :
					(getMethodInfo != null && getMethodInfo.IsFamily) || (setMethodInfo != null && setMethodInfo.IsFamily) ? Modifiers.Protected :
					Modifiers.Private;
				var getOrSet = getMethodInfo ?? setMethodInfo;
				if (getOrSet.IsAbstract)
					modifiers |= Modifiers.Abstract;
				if (getOrSet.IsVirtual)
					modifiers |= Modifiers.Virtual;
				if (getOrSet.IsStatic)
					modifiers |= Modifiers.Static;
				var baseDefinition = getOrSet.GetBaseDefinition();
				if (baseDefinition != null && baseDefinition.DeclaringType != getOrSet.DeclaringType)
					modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
				break;

			case MemberTypes.Event:
				var eventInfo = (EventInfo) info;
				var addMethodInfo = eventInfo.GetAddMethod(true);
				var removeMethodInfo = eventInfo.GetRemoveMethod(true);
				modifiers =
					(addMethodInfo != null && addMethodInfo.IsPublic) || (removeMethodInfo != null && removeMethodInfo.IsPublic) ? Modifiers.Public :
					(addMethodInfo != null && addMethodInfo.IsFamilyOrAssembly) || (removeMethodInfo != null && removeMethodInfo.IsFamilyOrAssembly) ? Modifiers.Internal | Modifiers.Protected :
					(addMethodInfo != null && addMethodInfo.IsAssembly) || (removeMethodInfo != null && removeMethodInfo.IsAssembly) ? Modifiers.Internal :
					(addMethodInfo != null && addMethodInfo.IsFamily) || (removeMethodInfo != null && removeMethodInfo.IsFamily) ? Modifiers.Protected :
					Modifiers.Private;
				var addOrRemove = addMethodInfo ?? removeMethodInfo;
				if (addOrRemove.IsAbstract)
					modifiers |= Modifiers.Abstract;
				if (addOrRemove.IsVirtual)
					modifiers |= Modifiers.Virtual;
				if (addOrRemove.IsStatic)
					modifiers |= Modifiers.Static;
				baseDefinition = addOrRemove.GetBaseDefinition();
				if (baseDefinition != null && baseDefinition.DeclaringType != addOrRemove.DeclaringType)
					modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
				break;

			default:
				break;
		}
		accessLevel = AccessLevelFromModifiers(modifiers);

		memberInfo = info;
		var generic = info.Name.IndexOf('`');
		name = generic < 0 ? info.Name : info.Name.Substring(0, generic);
		parentSymbol = memberOf;
		switch (info.MemberType)
		{
			case MemberTypes.Field:
				kind = ((FieldInfo) info).IsLiteral ?
					(memberOf.kind == SymbolKind.Enum ? SymbolKind.EnumMember : SymbolKind.ConstantField) :
					SymbolKind.Field;
				break;
			case MemberTypes.Property:
				var indexParams = ((PropertyInfo) info).GetIndexParameters();
				kind = indexParams.Length > 0 ? SymbolKind.Indexer : SymbolKind.Property;
				break;
			case MemberTypes.Event:
				kind = SymbolKind.Event;
				break;
			default:
				throw new InvalidOperationException("Importing a non-supported member type!");
		}
	}

	public override SymbolDefinition TypeOf()
	{
		if (memberInfo.MemberType == MemberTypes.Constructor)
			return parentSymbol.TypeOf();
		
		if (type != null && !type.definition.IsValid())
			type = null;
		
		if (type == null)
		{
			Type memberType = null;
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Field:
					memberType = ((FieldInfo) memberInfo).FieldType;
					break;
				case MemberTypes.Property:
					memberType = ((PropertyInfo) memberInfo).PropertyType;
					break;
				case MemberTypes.Event:
					memberType = ((EventInfo) memberInfo).EventHandlerType;
					break;
				case MemberTypes.Method:
					memberType = ((MethodInfo) memberInfo).ReturnType;
					break;
			}
			type = ReflectedTypeReference.ForType(memberType);
		}

		return type != null ? type.definition : unknownType;
	}

	//public override bool IsStatic
	//{
	//    get
	//    {
	//        switch (memberInfo.MemberType)
	//        {
	//            case MemberTypes.Method:
	//                return ((MethodInfo) memberInfo).IsStatic;
	//            case MemberTypes.Field:
	//                return ((FieldInfo) memberInfo).IsStatic;
	//            case MemberTypes.Property:
	//                return ((PropertyInfo) memberInfo).GetGetMethod(true).IsStatic;
	//            case MemberTypes.NestedType:
	//                return false; // TODO: Fix this!!!
	//            default:
	//                return false;
	//        }
	//    }
	//    set { }
	//}

	//public override bool IsPublic
	//{
	//    get
	//    {
	//        switch (memberInfo.MemberType)
	//        {
	//            case MemberTypes.Method:
	//                return ((MethodInfo) memberInfo).IsPublic;
	//            case MemberTypes.Field:
	//                return ((FieldInfo) memberInfo).IsPublic;
	//            case MemberTypes.Property:
	//                return ((PropertyInfo) memberInfo).GetGetMethod(true).IsPublic;
	//            case MemberTypes.NestedType:
	//                return ((Type) memberInfo).IsPublic;
	//            default:
	//                return false;
	//        }
	//    }
	//    set { }
	//}

	//public override bool IsProtected
	//{
	//    get
	//    {
	//        switch (memberInfo.MemberType)
	//        {
	//            case MemberTypes.Method:
	//                return ((MethodInfo) memberInfo).IsFamily;
	//            case MemberTypes.Field:
	//                return ((FieldInfo) memberInfo).IsFamily;
	//            case MemberTypes.Property:
	//                return ((PropertyInfo) memberInfo).GetGetMethod(true).IsFamily;
	//            case MemberTypes.NestedType:
	//                return ((Type) memberInfo).IsNestedFamily;
	//            default:
	//                return false;
	//        }
	//    }
	//    set { }
	//}
}


public class ReflectedTypeReference : SymbolReference
{
	protected Type reflectedType;
	protected ReflectedTypeReference(Type type)
	{
		reflectedType = type;
	}

	private static readonly Dictionary<Type, ReflectedTypeReference> allReflectedReferences = new Dictionary<Type,ReflectedTypeReference>();

	public static ReflectedTypeReference ForType(Type type)
	{
		ReflectedTypeReference result;
		if (allReflectedReferences.TryGetValue(type, out result))
			return result;
		result = new ReflectedTypeReference(type);
		allReflectedReferences[type] = result;
		return result;
	}

	public override SymbolDefinition definition
	{
		get
		{
			if (_definition != null && !_definition.IsValid())
				_definition = null;
			
			if (_definition == null)
			{
				if (reflectedType.IsArray)
				{
					var elementType = reflectedType.GetElementType();
					var elementTypeDefinition = ReflectedTypeReference.ForType(elementType).definition as TypeDefinitionBase;
					var rank = reflectedType.GetArrayRank();
					_definition = elementTypeDefinition.MakeArrayType(rank);
					return _definition;
				}

				if (reflectedType.IsGenericParameter)
				{
					var index = reflectedType.GenericParameterPosition;
					var reflectedDeclaringMethod = reflectedType.DeclaringMethod;
					if (reflectedDeclaringMethod != null)
					{
						var declaringTypeRef = ForType(reflectedDeclaringMethod.DeclaringType);
						var declaringType = declaringTypeRef.definition as ReflectedType;
						if (declaringType == null)
							return _definition = SymbolDefinition.unknownType;
						var methodName = reflectedDeclaringMethod.Name;
						var genericMarker = methodName.IndexOf('`');
						var numTypeArgs = 0;
						if (genericMarker > 0)
						{
							numTypeArgs = int.Parse(methodName.Substring(genericMarker + 1));
							methodName = methodName.Substring(0, genericMarker);
						}
						var member = declaringType.FindName(methodName, numTypeArgs, false);
						if (member != null && member.kind == SymbolKind.MethodGroup)
						{
							var methodGroup = (MethodGroupDefinition) member;
							foreach (var m in methodGroup.methods)
							{
								var reflectedMethod = m as ReflectedMethod;
								if (reflectedMethod != null && reflectedMethod.reflectedMethodInfo == reflectedDeclaringMethod)
								{
									member = reflectedMethod;
									break;
								}
							}
						}
						var methodDefinition = member as MethodDefinition;
						_definition = (methodDefinition != null && methodDefinition.typeParameters != null
							? methodDefinition.typeParameters.ElementAtOrDefault(index) : null)
							?? SymbolDefinition.unknownSymbol;
					}
					else
					{
						var reflectedDeclaringType = reflectedType.DeclaringType;
						while (true)
						{
							var parentType = reflectedDeclaringType.DeclaringType;
							if (parentType == null)
								break;
							var count = parentType.GetGenericArguments().Length;
							if (count <= index)
							{
								index -= count;
								break;
							}
							reflectedDeclaringType = parentType;
						}

						var declaringTypeRef = ForType(reflectedDeclaringType);
						var declaringType = declaringTypeRef.definition as TypeDefinition;
						if (declaringType == null)
							return _definition = SymbolDefinition.unknownType;

						_definition = declaringType.typeParameters[index];
					}
					return _definition;
				}
				if (reflectedType.IsNested)
				{
					var parentType = ForType(reflectedType.DeclaringType);
					_definition = parentType.definition.ImportReflectedType(reflectedType);
					return _definition;
				}
				if (reflectedType.IsGenericType && !reflectedType.IsGenericTypeDefinition)
				{
					var reflectedTypeDef = reflectedType.GetGenericTypeDefinition();
					var genericTypeDefRef = ForType(reflectedTypeDef);
					var genericTypeDef = genericTypeDefRef.definition as TypeDefinition;
					if (genericTypeDef == null)
						return _definition = SymbolDefinition.unknownType;

					var reflectedTypeArgs = reflectedType.GetGenericArguments();
					var numGenericArgs = reflectedTypeArgs.Length;
					var declaringType = reflectedType.DeclaringType;
					if (declaringType != null && declaringType.IsGenericType)
					{
						var parentArgs = declaringType.GetGenericArguments();
						numGenericArgs -= parentArgs.Length;
					}

					var typeArguments = new ReflectedTypeReference[numGenericArgs];
					for (int i = typeArguments.Length - numGenericArgs, j = 0; i < typeArguments.Length; ++i)
						typeArguments[j++] = ForType(reflectedTypeArgs[i]);
					_definition = genericTypeDef.ConstructType(typeArguments);
					return _definition;
				}

				var assemblyDefinition = AssemblyDefinition.FromAssembly(reflectedType.Assembly);
				var result = assemblyDefinition.FindNamespace(reflectedType.Namespace);
				//var typeNames = reflectedType.ToString().Substring(reflectedType.ToString().LastIndexOf('.') + 1).Split('+');
				//foreach (var tn in typeNames)
				var tn = reflectedType.Name;
				{
					var rankSpecifier = tn.IndexOf('[');
					var def = result.FindName(rankSpecifier > 0 ? tn.Substring(0, rankSpecifier) : tn, 0, true);
					if (def == null)
					{
						UnityEngine.Debug.LogWarning(tn + " not found in " + result + " " + result.GetHashCode() + "\n" + "while resolving reference to " + reflectedType);
					//	Debug.Log(result.Dump());
					//	break;
					}
					else if (rankSpecifier > 0)
					{
						var arrayType = def as TypeDefinition;
						if (arrayType != null)
							def = arrayType.MakeArrayType(tn.Length - rankSpecifier - 1);
						else
						{
							def = null;
						//	Debug.LogWarning(tn.Substring(0, rankSpecifier) + " is not a type!");
						}
					}
					result = def;
				}
				if (result != null)
					_definition = result;
				//else
				//	Debug.Log(reflectedType.FullName + " not found");
				if (_definition == null)
					_definition = SymbolDefinition.unknownType;
			}
			return _definition;
		}
	}

	public override string ToString()
	{
		return reflectedType.FullName;
	}
}

public class ReflectedMethod : MethodDefinition
{
	public readonly MethodInfo reflectedMethodInfo;

	public ReflectedMethod(MethodInfo methodInfo, SymbolDefinition memberOf)
	{
		modifiers =
			methodInfo.IsPublic ? Modifiers.Public :
			methodInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
			methodInfo.IsAssembly ? Modifiers.Internal :
			methodInfo.IsFamily ? Modifiers.Protected :
			Modifiers.Private;
		if (methodInfo.IsAbstract)
			modifiers |= Modifiers.Abstract;
		if (methodInfo.IsVirtual)
			modifiers |= Modifiers.Virtual;
		if (methodInfo.IsStatic)
			modifiers |= Modifiers.Static;
		if (methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType)
			modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
		if (methodInfo.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), true))
		{
	//		Debug.Log(methodInfo + " is extension method declared in " + methodInfo.DeclaringType.FullName);
			var assemblyDefinition = AssemblyDefinition.FromAssembly(methodInfo.DeclaringType.Assembly);
			NamespaceOfExtensionMethod = assemblyDefinition.FindNamespace(methodInfo.DeclaringType.Namespace) as NamespaceDefinition;
		}
		accessLevel = AccessLevelFromModifiers(modifiers);

		reflectedMethodInfo = methodInfo;
		var genericMarker = methodInfo.Name.IndexOf('`');
		name = genericMarker < 0 ? methodInfo.Name : methodInfo.Name.Substring(0, genericMarker);
		parentSymbol = memberOf;

		var tp = methodInfo.GetGenericArguments();
		if (tp.Length > 0)
		{
			var numGenericArgs = tp.Length;
		//	Debug.Log(methodInfo.Name + " with " + tp.Length + " generic arguments.");
			typeParameters = new List<TypeParameterDefinition>(tp.Length);
			for (var i = tp.Length - numGenericArgs; i < tp.Length; ++i)
			{
				var tpDef = new TypeParameterDefinition { kind = SymbolKind.TypeParameter, name = tp[i].Name, parentSymbol = this };
				typeParameters.Add(tpDef);
			}
		}

		returnType = ReflectedTypeReference.ForType(methodInfo.ReturnType);

		if (parameters == null)
			parameters = new List<ParameterDefinition>();
		foreach (var p in methodInfo.GetParameters())
		{
			var isByRef = p.ParameterType.IsByRef;
			var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			var parameterToAdd = new ParameterDefinition
			{
			    kind = SymbolKind.Parameter,
				parentSymbol = this,
				name = p.Name,
				type = ReflectedTypeReference.ForType(parameterType),
				modifiers = isByRef ? (p.IsOut ? Modifiers.Out : Modifiers.Ref) : Attribute.IsDefined(p, typeof(ParamArrayAttribute)) ? Modifiers.Params : Modifiers.None,
			};
			if (p.RawDefaultValue != DBNull.Value)
			{
				//var dv = Attribute.GetCustomAttribute(p, typeof(System.ComponentModel.DefaultValueAttribute));
				parameterToAdd.defaultValue = p.RawDefaultValue == null ? "null" : p.RawDefaultValue.ToString();
			}
			parameters.Add(parameterToAdd);
		}
	}
}

public class ReflectedConstructor : MethodDefinition
{
	//private readonly ConstructorInfo reflectedConstructorInfo;

	public ReflectedConstructor(ConstructorInfo constructorInfo, SymbolDefinition memberOf)
	{
		modifiers =
			constructorInfo.IsPublic ? Modifiers.Public :
			constructorInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
			constructorInfo.IsAssembly ? Modifiers.Internal :
			constructorInfo.IsFamily ? Modifiers.Protected :
			Modifiers.Private;
		if (constructorInfo.IsAbstract)
			modifiers |= Modifiers.Abstract;
		if (constructorInfo.IsStatic)
			modifiers |= Modifiers.Static;
		accessLevel = AccessLevelFromModifiers(modifiers);

		//reflectedConstructorInfo = constructorInfo;
		//var genericMarker = methodInfo.Name.IndexOf('`');
		//name = genericMarker < 0 ? methodInfo.Name : methodInfo.Name.Substring(0, genericMarker);
		name = ".ctor";
		kind = SymbolKind.Constructor;
		parentSymbol = memberOf;

		returnType = new SymbolReference(memberOf);

		if (parameters == null)
			parameters = new List<ParameterDefinition>();
		foreach (var p in constructorInfo.GetParameters())
		{
			var isByRef = p.ParameterType.IsByRef;
			var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			var parameterToAdd = new ParameterDefinition
			{
				kind = SymbolKind.Parameter,
				parentSymbol = this,
				name = p.Name,
				type = ReflectedTypeReference.ForType(parameterType),
				modifiers = isByRef ? (p.IsOut ? Modifiers.Out : Modifiers.Ref) : Attribute.IsDefined(p, typeof(ParamArrayAttribute)) ? Modifiers.Params : Modifiers.None,
			};
			if (p.RawDefaultValue != DBNull.Value)
			{
				//var dv = Attribute.GetCustomAttribute(p, typeof(System.ComponentModel.DefaultValueAttribute));
				parameterToAdd.defaultValue = p.RawDefaultValue.ToString();
			}
			parameters.Add(parameterToAdd);
		}
	}
}

public class ReflectedType : TypeDefinition
{
	private readonly Type reflectedType;

	private bool allPublicMembersReflected;
	private bool allNonPublicMembersReflected;

//	private static Dictionary<Type, ReflectedType> allReflectedTypes;

	public ReflectedType(Type type)
	{
		reflectedType = type;
		modifiers = type.IsNested ?
			(	type.IsNestedPublic ? Modifiers.Public :
				type.IsNestedFamORAssem ? Modifiers.Internal | Modifiers.Protected :
				type.IsNestedAssembly ? Modifiers.Internal :
				type.IsNestedFamily ? Modifiers.Protected :
				Modifiers.Private)
			:
			(	type.IsPublic ? Modifiers.Public :
				!type.IsVisible ? Modifiers.Internal :
				Modifiers.Private );
		if (type.IsAbstract && type.IsSealed)
			modifiers |= Modifiers.Static;
		else if (type.IsAbstract)
			modifiers |= Modifiers.Abstract;
		else if (type.IsSealed)
			modifiers |= Modifiers.Sealed;
		accessLevel = AccessLevelFromModifiers(modifiers);

		var assemblyDefinition = AssemblyDefinition.FromAssembly(type.Assembly);

		var generic = type.Name.IndexOf('`');
		name = generic < 0 ? type.Name : type.Name.Substring(0, generic);
		name = name.Replace("[*]", "[]");
		parentSymbol = string.IsNullOrEmpty(type.Namespace) ? assemblyDefinition.GlobalNamespace : assemblyDefinition.FindNamespace(type.Namespace);
		if (type.IsInterface)
			kind = SymbolKind.Interface;
		else if (type.IsEnum)
			kind = SymbolKind.Enum;
		else if (type.IsValueType)
			kind = SymbolKind.Struct;
		else if (type.IsClass)
		{
			kind = SymbolKind.Class;
			if (type.BaseType == typeof(System.MulticastDelegate))
			{
				kind = SymbolKind.Delegate;
			}
		}
		else
			kind = SymbolKind.None;

//		if (type.IsArray)
//			Debug.LogError("ReflectedType is Array " + name);

		//if (!type.IsGenericTypeDefinition && type.IsGenericType)
		//	UnityEngine.Debug.LogError("Creating ReflectedType instead of ConstructedTypeDefinition from " + type.FullName);

		if (type.IsGenericTypeDefinition)// || type.IsGenericType)
		{
			var gtd = type.GetGenericTypeDefinition() ?? type;
			var tp = gtd.GetGenericArguments();
			var numGenericArgs = tp.Length;
			var declaringType = gtd.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				var parentArgs = declaringType.GetGenericArguments();
				numGenericArgs -= parentArgs.Length;
			}

			if (numGenericArgs > 0)
			{
				typeParameters = new List<TypeParameterDefinition>(numGenericArgs);
				for (var i = tp.Length - numGenericArgs; i < tp.Length; ++i)
				{
					var tpDef = new TypeParameterDefinition { kind = SymbolKind.TypeParameter, name = tp[i].Name, parentSymbol = this };
					typeParameters.Add(tpDef);
				}
			}
		}

		if (type.BaseType != null)
		{
			baseType = ReflectedTypeReference.ForType(type.BaseType);
		}

		interfaces = new List<SymbolReference>();
		var implements = type.GetInterfaces();
		for (var i = 0; i < implements.Length; ++i)
			interfaces.Add(ReflectedTypeReference.ForType(implements[i]));
	}

	private Dictionary<int, SymbolDefinition> importedMembers;
	public SymbolDefinition ImportReflectedMember(MemberInfo info)
	{
		if (info.MemberType == MemberTypes.Method && ((MethodInfo) info).IsPrivate)
			return null;
		if (info.MemberType == MemberTypes.Constructor && ((ConstructorInfo) info).IsPrivate)
			return null;
		if (info.MemberType == MemberTypes.Field && (((FieldInfo) info).IsPrivate || kind == SymbolKind.Enum && info.Name == "value__"))
			return null;
		if (info.MemberType == MemberTypes.Property)
		{
			var p = (PropertyInfo) info;
			var get = p.GetGetMethod();
			var set = p.GetSetMethod();
			if ((get == null || get.IsPrivate) && (set == null || set.IsPrivate))
				return null;
		}
		if (info.MemberType == MemberTypes.Event)
		{
			var e = (EventInfo) info;
			var add = e.GetAddMethod();
			var remove = e.GetRemoveMethod();
			if ((add == null || add.IsPrivate) && (remove == null || remove.IsPrivate))
				return null;
		}
		//if (info.Name.IndexOf('.', 1) > 0)
		//{
		//	Debug.Log("m.Name");
		//}
		
		SymbolDefinition imported = null;

		if (importedMembers == null)
			importedMembers = new Dictionary<int, SymbolDefinition>();
		else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
			return imported;

		if (info.MemberType == MemberTypes.NestedType || info.MemberType == MemberTypes.TypeInfo)
		{
			imported = ImportReflectedType(info as Type);
		}
		else if (info.MemberType == MemberTypes.Method)
		{
			imported = ImportReflectedMethod(info as MethodInfo);
		}
		else if (info.MemberType == MemberTypes.Constructor)
		{
			imported = ImportReflectedConstructor(info as ConstructorInfo);
		}
		else
		{
			imported = new ReflectedMember(info, this);
		}
		
		members[imported.ReflectionName] = imported;
		importedMembers[info.MetadataToken] = imported;
		return imported;
	}

	public override string GetName()
	{
		if (builtInTypes.ContainsValue(this))
			return (from x in builtInTypes where x.Value == this select x.Key).First();
		return base.GetName();
	}

	public override SymbolDefinition TypeOf()
	{
		if (kind != SymbolKind.Delegate)
			return this;
		
		GetParameters();
		return returnType.definition;
	}

	public override List<SymbolDefinition> GetAllIndexers()
	{
		if (!allPublicMembersReflected || !allNonPublicMembersReflected)
			ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
		
		return base.GetAllIndexers();
	}

	//protected string RankString()
	//{
	//    return reflectedType.IsArray ? '[' + new string(',', reflectedType.GetArrayRank() - 1) + ']' : string.Empty;
	//}
	
	//public override TypeDefinition MakeArrayType(int rank)
	//{
	////	Debug.LogWarning("MakeArrayType " + this + RankString());
	////	if (rank == 1)
	//        return ImportReflectedType(reflectedType.MakeArrayType(rank));
	////	return new ArrayTypeDefinition(this, rank) { kind = kind };
	//}

	private static bool FilterByName(MemberInfo m, object filterCriteria)
	{
		var memberName = (string)filterCriteria;
		return m.Name == memberName || m.Name.Length > memberName.Length && m.Name.StartsWith(memberName) && m.Name[memberName.Length] == '`';
	}

	public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName = DecodeId(memberName);
		
		SymbolDefinition member = null;
		if (numTypeParameters > 0)
			memberName += "`" + numTypeParameters;
		if (!members.TryGetValue(memberName, out member) && (!allPublicMembersReflected || !allNonPublicMembersReflected))
		{
			var findResult = reflectedType.FindMembers(MemberTypes.All, BindingFlags.Public | BindingFlags.Instance, FilterByName, memberName);
			if (findResult.Length == 0)
				findResult = reflectedType.FindMembers(MemberTypes.All, BindingFlags.Public | BindingFlags.Static, FilterByName, memberName);
//			if (findResult.Length == 0)
//			{
//				var baseDefinition = BaseType();
//				if (baseDefinition != null)
//				{
//					var rt = baseDefinition as ReflectedType;
//					if (rt != null)
//						rt.ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
//					member = baseDefinition.FindName(memberName, numTypeParameters, asTypeOnly);
//					if (asTypeOnly && member != null && !(member is TypeDefinitionBase))
//						return null;
//					return member;
//				}
//			}
//			else
			if (findResult.Length > 0)
			{
				member = ImportReflectedMember(findResult[0]);
				for (var i = 1; i < findResult.Length; ++i)
					ImportReflectedMember(findResult[i]);
			}

//			if (member != null)
//				members[member.ReflectionName] = member;
		}
		if (asTypeOnly && member != null && !(member is TypeDefinitionBase))
			return null;
		return member;
	}

	public void ReflectAllMembers(BindingFlags flags)
	{
		var instaceMembers = reflectedType.GetMembers(flags | BindingFlags.Instance);
		foreach (var m in instaceMembers)
			if (m.MemberType != MemberTypes.Method || !((MethodInfo) m).IsSpecialName)
				ImportReflectedMember(m);

		var staticMembers = reflectedType.GetMembers(flags | BindingFlags.Static);
		foreach (var m in staticMembers)
			if (m.MemberType != MemberTypes.Method || !((MethodInfo) m).IsSpecialName)
				ImportReflectedMember(m);

		if ((flags & BindingFlags.Public) == BindingFlags.Public)
			allPublicMembersReflected = true;
		if ((flags & BindingFlags.NonPublic) == BindingFlags.NonPublic)
			allNonPublicMembersReflected = true;
	}

	public override string GetTooltipText()
	{
		if (kind != SymbolKind.Delegate)
			return base.GetTooltipText();

		//if (tooltipText == null)
		{
			tooltipText = base.GetTooltipText();
			
			tooltipText += "\n\nDelegate info\n";
			tooltipText += GetDelegateInfoText();
			
			var xmlDocs = GetXmlDocs();
			if (!string.IsNullOrEmpty(xmlDocs))
			{
				tooltipText += "\n\n" + xmlDocs;
			}
		}

		return tooltipText;
	}

	private ReflectedTypeReference returnType;
	private List<ParameterDefinition> parameters;
	public override List<ParameterDefinition> GetParameters()
	{
		if (kind != SymbolKind.Delegate)
			return null;
		
		if (parameters == null)
		{
			var invoke = reflectedType.GetMethod("Invoke");
			
			returnType = ReflectedTypeReference.ForType(invoke.ReturnType);
			
			parameters = new List<ParameterDefinition>();
			foreach (var p in invoke.GetParameters())
			{
				var isByRef = p.ParameterType.IsByRef;
				var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
				parameters.Add(new ParameterDefinition
				{
					kind = SymbolKind.Parameter,
					parentSymbol = this,
					name = p.Name,
					type = ReflectedTypeReference.ForType(parameterType),
					modifiers = isByRef ? (p.IsOut ? Modifiers.Out : Modifiers.Ref) : Attribute.IsDefined(p, typeof(ParamArrayAttribute)) ? Modifiers.Params : Modifiers.None,
				});
			}
		}
		
		return parameters;
	}

	private string delegateInfoText;
	public override string GetDelegateInfoText()
	{
		if (delegateInfoText == null)
		{
			var parameters = GetParameters();
			var returnType = TypeOf();
			
			delegateInfoText = returnType.GetName() + " " + GetName() + (parameters.Count == 1 ? "( " : "(");
			delegateInfoText += PrintParameters(parameters) + (parameters.Count == 1 ? " )" : ")");
		}

		return delegateInfoText;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		if (!allPublicMembersReflected)
		{
			if (!allNonPublicMembersReflected)
				ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
			else
				ReflectAllMembers(BindingFlags.Public);
		}
		else if (!allNonPublicMembersReflected)
		{
			ReflectAllMembers(BindingFlags.NonPublic);
		}

		base.ResolveMember(leaf, context, numTypeArgs);
	}

	private Dictionary<BindingFlags, Dictionary<string, SymbolDefinition>> cachedMemberCompletions;
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		if (!allPublicMembersReflected)
		{
			if (!allNonPublicMembersReflected && ((mask & AccessLevelMask.NonPublic) != 0 || (flags & BindingFlags.NonPublic) != 0))
				ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
			else
				ReflectAllMembers(BindingFlags.Public);
		}
		else if (!allNonPublicMembersReflected && ((mask & AccessLevelMask.NonPublic) != 0 || (flags & BindingFlags.NonPublic) != 0))
		{
			ReflectAllMembers(BindingFlags.NonPublic);
		}
		
		base.GetMembersCompletionData(data, flags, mask, assembly);

		//if ((mask & AccessLevelMask.Public) != 0)
		//{
		//	if (assembly.InternalsVisibleIn(this.Assembly))
		//		mask |= AccessLevelMask.Internal;
		//	else
		//		mask &= ~AccessLevelMask.Internal;
		//}
		
		//if (cachedMemberCompletions == null)
		//	cachedMemberCompletions = new Dictionary<BindingFlags, Dictionary<string, SymbolDefinition>>();
		//if (!cachedMemberCompletions.ContainsKey(flags))
		//{
		//	var cache = cachedMemberCompletions[flags] = new Dictionary<string, SymbolDefinition>();
		//	base.GetMembersCompletionData(cache, flags, mask, assembly);
		//}

		//var completions = cachedMemberCompletions[flags];
		//foreach (var entry in completions)
		//	if (entry.Value.IsAccessible(mask) && !data.ContainsKey(entry.Key))
		//		data.Add(entry.Key, entry.Value);
	}
}

public class ConstructedNestedTypeDefinition : TypeDefinition
{
	public readonly TypeDefinition genericSymbol;

	public ConstructedNestedTypeDefinition(TypeDefinition genericSymbolDefinition)
	{
		genericSymbol = genericSymbolDefinition;
		kind = genericSymbol.kind;
		modifiers = genericSymbol.modifiers;
		accessLevel = genericSymbol.accessLevel;
		name = genericSymbol.name;
		typeParameters = genericSymbol.typeParameters;
	}

	public override SymbolDefinition TypeOf()
	{
		//var definingType = ((ConstructedTypeDefinition) parentSymbol);
		//var genType = definingType.genericTypeDefinition;
		//var oldTypeParams = genType.tempTypeArguments;
		//genType.tempTypeArguments = definingType.typeArguments;

		var result = genericSymbol.TypeOf() as TypeDefinitionBase;
		var tp = result as TypeParameterDefinition;
		if (tp != null)
			result = parentSymbol.TypeOfTypeParameter(tp);
		//UnityEngine.Debug.LogWarning(result);

		//genType.tempTypeArguments = oldTypeParams;
		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return genericSymbol;
	}

	public override string GetTooltipText()
	{
		//var definingType = ((ConstructedTypeDefinition) parentSymbol);
		//var genType = definingType.genericTypeDefinition;
		//var oldTypeParams = genType.tempTypeArguments;
		//genType.tempTypeArguments = definingType.typeArguments;

		var result = genericSymbol.GetTooltipText();

		//genType.tempTypeArguments = oldTypeParams;
		return result;
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return false;
	//	}
	//}
}

public class ConstructedInstanceDefinition : InstanceDefinition
{
	public readonly InstanceDefinition genericSymbol;

	public ConstructedInstanceDefinition(InstanceDefinition genericSymbolDefinition)
	{
		genericSymbol = genericSymbolDefinition;
		kind = genericSymbol.kind;
		modifiers = genericSymbol.modifiers;
		accessLevel = genericSymbol.accessLevel;
		name = genericSymbol.name;
	}

	public override SymbolDefinition TypeOf()
	{
		var result = genericSymbol.TypeOf() as TypeDefinitionBase;

		var ctx = parentSymbol as ConstructedTypeDefinition;
		if (ctx != null)
			result = result.SubstituteTypeParameters(ctx);

		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return genericSymbol;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		var symbolType = TypeOf() as TypeDefinitionBase;
		if (symbolType != null)
			symbolType.ResolveMember(leaf, context, numTypeArgs);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		var symbolType = TypeOf();
		if (symbolType != null)
			symbolType.GetMembersCompletionData(data, BindingFlags.Instance, mask, assembly);
	}
}

public class InstanceDefinition : SymbolDefinition
{
	public SymbolReference type;
	public override SymbolDefinition TypeOf()
	{
		if (type != null && (type.definition == null || !type.definition.IsValid()))
			type = null;
		
		if (type == null)
		{
			//type = new SymbolReference();

		//	var parentDefinition = parentScope as SymbolDefinition;
		//	if (parentDefinition.declarations.Count > 0)
			{
				var decl = declarations != null ? declarations.FirstOrDefault() : null;
				if (decl != null)
				{
					ParseTree.BaseNode typeNode = null;
					switch (decl.kind)
					{
						case SymbolKind.Parameter:
							typeNode = decl.parseTreeNode.FindChildByName("type");
							type = typeNode != null ? new SymbolReference(typeNode) : null;//"System.Object" };
							break;

						case SymbolKind.Field:
							typeNode = decl.parseTreeNode.parent.parent.parent.FindChildByName("type");
							type = typeNode != null ? new SymbolReference(typeNode) : null;//"System.Object" };
							break;

						case SymbolKind.EnumMember:
							type = new SymbolReference(parentSymbol);
							break;

						case SymbolKind.ConstantField:
						case SymbolKind.LocalConstant:
							//typeNode = decl.parseTreeNode.parent.parent.ChildAt(1);
							//break;
							switch (decl.parseTreeNode.parent.parent.RuleName)
							{
								case "constantDeclaration":
								case "localConstantDeclaration":
									typeNode = decl.parseTreeNode.parent.parent.ChildAt(1);
									break;

								default:
									typeNode = decl.parseTreeNode.parent.parent.parent.FindChildByName("IDENTIFIER");
									break;
							}
							type = typeNode != null ? new SymbolReference(typeNode) : null;
							break;

						case SymbolKind.Property:
						case SymbolKind.Indexer:
							typeNode = decl.parseTreeNode.parent.FindChildByName("type");
							type = typeNode != null ? new SymbolReference(typeNode) : null;
							break;

						case SymbolKind.Event:
							typeNode = decl.parseTreeNode.FindParentByName("eventDeclaration").ChildAt(1);
							type = typeNode != null ? new SymbolReference(typeNode) : null;
							break;
						
						case SymbolKind.Variable:
							if (decl.parseTreeNode != null && decl.parseTreeNode.parent != null && decl.parseTreeNode.parent.parent != null)
								typeNode = decl.parseTreeNode.parent.parent.FindChildByName("localVariableType");
							type = typeNode != null ? new SymbolReference(typeNode) : null;
							break;

						case SymbolKind.ForEachVariable:
							if (decl.parseTreeNode != null)
								typeNode = decl.parseTreeNode.FindChildByName("localVariableType");
							type = typeNode != null ? new SymbolReference(typeNode) : null;
							break;

						case SymbolKind.FromClauseVariable:
							type = null;
							if (decl.parseTreeNode != null)
							{
								typeNode = decl.parseTreeNode.FindChildByName("type");
								type = typeNode != null
									? new SymbolReference(typeNode)
									: new SymbolReference(EnumerableElementType(decl.parseTreeNode.NodeAt(-1)));
							}
							break;

						case SymbolKind.CatchParameter:
							if (decl.parseTreeNode != null)
								typeNode = decl.parseTreeNode.parent.FindChildByName("exceptionClassType");
							type = typeNode != null ? new SymbolReference(typeNode) : null;
							break;
					}
				}
			}
		}

		return type != null ? type.definition : unknownType;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		TypeOf();
		if (type == null || type.definition == null || type.definition == unknownType || type.definition == unknownSymbol)
		{
			leaf.resolvedSymbol = null;
			return;
		}
		type.definition.ResolveMember(leaf, context, numTypeArgs);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		var instanceType = TypeOf();
		if (instanceType != null)
			instanceType.GetMembersCompletionData(data, BindingFlags.Instance, mask, assembly);
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return TypeOf().IsGeneric;
	//	}
	//}
}

public class IndexerDefinition : InstanceDefinition
{
	public List<ParameterDefinition> parameters;

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition) Create(symbol);
		parameter.type = new SymbolReference(symbol.parseTreeNode.FindChildByName("type"));
		parameter.parentSymbol = this;
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (parameters == null)
				parameters = new List<ParameterDefinition>();
			parameters.Add(parameter);
		}
		return parameter;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			symbol.definition = definition;
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition) symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}
	
	public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName = DecodeId(memberName);
		
		if (!asTypeOnly && parameters != null)
		{
			var definition = parameters.Find(x => x.name == memberName);
			if (definition != null)
				return definition;
		}
		return base.FindName(memberName, numTypeParameters, asTypeOnly);
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		if (parameters != null)
		{
			var leafText = leaf.token.text;
			var definition = parameters.Find(x => x.name == leafText);
			if (definition != null)
			{
				leaf.resolvedSymbol = definition;
				return;
			}
		}
		base.ResolveMember(leaf, context, numTypeArgs);
	}
}

public class ThisReference : InstanceDefinition
{
	public ThisReference(SymbolReference type)
	{
		this.type = type;
		kind = SymbolKind.Instance;
	}

	public override string GetTooltipText()
	{
		return type.definition.GetTooltipText();
	}
}

public class ValueParameter : ParameterDefinition {}

public class NullLiteral : InstanceDefinition
{
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
	}
}

public class ParameterDefinition : InstanceDefinition
{
	public bool IsThisParameter { get { return modifiers == Modifiers.This; } }

	public bool IsRef { get { return modifiers == Modifiers.Ref; } }
	public bool IsOut { get { return modifiers == Modifiers.Out; } }
	public bool IsParametersArray { get { return modifiers == Modifiers.Params; } }

	public bool IsOptional { get { return defaultValue != null || IsParametersArray; } }
	public string defaultValue;
}

public abstract class TypeDefinitionBase : SymbolDefinition
{
	protected SymbolDefinition thisReferenceCache;

	public override SymbolDefinition TypeOf()
	{
		return this;
	}

	public virtual TypeDefinitionBase SubstituteTypeParameters(ConstructedTypeDefinition context)
	{
		return this;
	}
	
	public virtual List<SymbolReference> Interfaces()
	{
		return _emptyInterfaceList;
	}

	public virtual TypeDefinitionBase BaseType()
	{
		return builtInTypes["object"];
	}

	protected virtual string RankString()
	{
		return string.Empty;
	}

	protected MethodDefinition defaultConstructor;
	public virtual MethodDefinition GetDefaultConstructor()
	{
		if (defaultConstructor == null)
		{
			defaultConstructor = new MethodDefinition
			{
				kind = SymbolKind.Constructor,
				parentSymbol = this,
				name = ".ctor",
				accessLevel = accessLevel,
				modifiers = modifiers & (Modifiers.Public | Modifiers.Internal | Modifiers.Protected),
			};
		}
		return defaultConstructor;
	}

	private Dictionary<int, ArrayTypeDefinition> createdArrayTypes;
	public ArrayTypeDefinition MakeArrayType(int arrayRank)
	{
		ArrayTypeDefinition arrayType;
		if (createdArrayTypes == null)
			createdArrayTypes = new Dictionary<int, ArrayTypeDefinition>();
		if (!createdArrayTypes.TryGetValue(arrayRank, out arrayType))
			createdArrayTypes[arrayRank] = arrayType = new ArrayTypeDefinition(this, arrayRank);
		return arrayType;
	}

	private NullableTypeDefinition createdNullableType;
	public NullableTypeDefinition MakeNullableType()
	{
		if (createdNullableType == null)
			createdNullableType = new NullableTypeDefinition(this);
		return createdNullableType;
	}

	public SymbolDefinition GetThisInstance()
	{
		if (thisReferenceCache == null)
		{
			if (IsStatic)
				return thisReferenceCache = unknownType;
			thisReferenceCache = new ThisReference(new SymbolReference(this));
		}
		return thisReferenceCache;
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		base.ResolveMember(leaf, context, numTypeArgs);

		if (leaf.resolvedSymbol == null)
		{
			var baseType = BaseType();
			var interfaces = Interfaces();
			
			if (interfaces != null)
			{
				foreach (var i in interfaces)
				{
					i.definition.ResolveMember(leaf, context, numTypeArgs);
					if (leaf.resolvedSymbol != null)
						return;
				}
			}

			if (baseType != null)
				baseType.ResolveMember(leaf, context, numTypeArgs);
		}
	}
	
	public virtual bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		return DerivesFrom(otherType);
	}

	public virtual bool DerivesFrom(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return true;

		if (BaseType() != null)
			return BaseType().DerivesFrom(otherType);

		return false;
	}
	
	protected override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		var indexers = GetAllIndexers();
		
		// TODO: Resolve overloads
		
		return indexers != null ? indexers[0] : null;
	}

	public virtual List<SymbolDefinition> GetAllIndexers()
	{
		List<SymbolDefinition> indexers = null;
		foreach (var m in members.Values)
			if (m.kind == SymbolKind.Indexer)
			{
				if (indexers == null)
					indexers = new List<SymbolDefinition>();
				indexers.Add(m);
			}
		return indexers;
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		base.GetCompletionData(data, false, assembly);
		var baseType = BaseType();
		var interfaces = Interfaces();
		foreach (var i in interfaces)
			i.definition.GetMembersCompletionData(data, fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any & ~AccessLevelMask.Private, assembly);
		if (baseType != null)
			baseType.GetMembersCompletionData(data, fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any & ~AccessLevelMask.Private, assembly);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		base.GetMembersCompletionData(data, flags, mask, assembly);
		var baseType = BaseType();
		var interfaces = Interfaces();
		if (flags != BindingFlags.Static)
			foreach (var i in interfaces)
				i.definition.GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, assembly);
		if (baseType != null && (kind != SymbolKind.Enum || flags != BindingFlags.Static))
			baseType.GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, assembly);
	}

	public virtual bool CanConvertTo(TypeDefinitionBase otherType)
	{
		return DerivesFrom(otherType);
	}
}

public class DelegateTypeDefinition : TypeDefinition
{
	public SymbolReference returnType;
	public List<ParameterDefinition> parameters;

	public override TypeDefinitionBase BaseType()
	{
		if (baseType == null)
			baseType = ReflectedTypeReference.ForType(typeof(MulticastDelegate));
		return baseType.definition as TypeDefinitionBase;
	}
	
	public override List<SymbolReference> Interfaces()
	{
		if (interfaces == null)
			interfaces = BaseType().Interfaces();
		return interfaces;
	}

	public override SymbolDefinition TypeOf()
	{
		return returnType != null && returnType.definition.IsValid() ? returnType.definition : unknownType;
	}

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition) Create(symbol);
		parameter.type = new SymbolReference(symbol.parseTreeNode.FindChildByName("type"));
		parameter.parentSymbol = this;
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (parameters == null)
				parameters = new List<ParameterDefinition>();
			parameters.Add(parameter);
			
			var nameNode = symbol.NameNode();
			if (nameNode != null)
				nameNode.SetDeclaredSymbol(parameter);
		}
		return parameter;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			//	if (!members.TryGetValue(symbolName, out definition) || definition is ReflectedMember || definition is ReflectedType)
			//		definition = AddMember(symbol);

			symbol.definition = definition;
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition) symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		if (parameters != null)
		{
			var leafText = leaf.token.text;
			var definition = parameters.Find(x => x.name == leafText);
			if (definition != null)
			{
				leaf.resolvedSymbol = definition;
				return;
			}
		}
		base.ResolveMember(leaf, context, numTypeArgs);
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}

	//public override List<TypeParameterDefinition> GetTypeParameters()
	//{
	//	return null;// typeParameters ?? new List<TypeParameterDefinition>();
	//}

	private string delegateInfoText;
	public override string GetDelegateInfoText()
	{
		if (delegateInfoText == null)
		{
			delegateInfoText = returnType.definition.GetName() + " " + GetName() + (parameters != null && parameters.Count == 1 ? "( " : "(");
			delegateInfoText += PrintParameters(parameters) + (parameters != null && parameters.Count == 1 ? " )" : ")");
		}
		return delegateInfoText;
	}
}

public class TypeParameterDefinition : TypeDefinitionBase
{
	public SymbolReference baseTypeConstraint;
	public List<SymbolReference> interfacesConstraint;
	public bool classConstraint;
	public bool structConstraint;
	public bool newConstraint;

	public override string GetTooltipText()
	{
		//if (tooltipText == null)
		{
			tooltipText = name + " in " + parentSymbol.GetName();
			if (baseTypeConstraint != null)
				tooltipText += " where " + name + " : " + BaseType().GetName();
		}
		return tooltipText;
	}

	public override string GetName()
	{
		//var definingType = parentSymbol as TypeDefinition;
		//if (definingType != null && definingType.tempTypeArguments != null)
		//{
		//    var index = definingType.typeParameters.IndexOf(this);
		//    return definingType.tempTypeArguments[index].definition.GetName();
		//}
		return name;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(ConstructedTypeDefinition context)
	{
		return context.TypeOfTypeParameter(this);
	}

	public override TypeDefinitionBase BaseType()
	{
		if (baseTypeConstraint == null)
			return base.BaseType();
		return baseTypeConstraint.definition as TypeDefinitionBase ?? base.BaseType();
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return true;
	//	}
	//}
}

public class ConstructedTypeDefinition : TypeDefinition
{
	public readonly TypeDefinition genericTypeDefinition;
	public readonly SymbolReference[] typeArguments;

	public ConstructedTypeDefinition(TypeDefinition definition, SymbolReference[] arguments)
	{
		name = definition.name;
		kind = definition.kind;
		parentSymbol = definition.parentSymbol;
		genericTypeDefinition = definition;

		if (definition.typeParameters != null && arguments != null)
		{
			typeParameters = definition.typeParameters;
			typeArguments = new SymbolReference[typeParameters.Count];
			for (var i = 0; i < typeArguments.Length && i < arguments.Length; ++i)
				typeArguments[i] = arguments[i];
		}
	}

	public override ConstructedTypeDefinition ConstructType(SymbolReference[] typeArgs)
	{
		var result = genericTypeDefinition.ConstructType(typeArgs);
		result.parentSymbol = parentSymbol;
		return result;
	}
	
	public override SymbolDefinition TypeOf()
	{
		if (kind != SymbolKind.Delegate)
			return base.TypeOf();
		
		var result = genericTypeDefinition.TypeOf() as TypeDefinitionBase;
		result = result.SubstituteTypeParameters(this);
		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return genericTypeDefinition;
	}

	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		if (typeParameters != null)
		{
			var index = typeParameters.IndexOf(tp);
			if (index >= 0)
				return typeArguments[index].definition as TypeDefinitionBase ?? tp;
		}
		return base.TypeOfTypeParameter(tp);
	}

	public override TypeDefinitionBase SubstituteTypeParameters(ConstructedTypeDefinition context)
	{
		var target = this;
		var parentType = parentSymbol as TypeDefinitionBase;
		if (parentType != null)
		{
			parentType = parentType.SubstituteTypeParameters(context);
			var constructedParent = parentType as ConstructedTypeDefinition;
			if (constructedParent != null)
				target = constructedParent.GetConstructedMember(this.genericTypeDefinition) as ConstructedTypeDefinition;
		}

		if (typeArguments == null)
			return target;

		var constructNew = false;
		var newArguments = new SymbolReference[typeArguments.Length];
		for (var i = 0; i < newArguments.Length; ++i)
		{
			newArguments[i] = typeArguments[i];
			var original = typeArguments[i].definition as TypeDefinitionBase;
			if (original == null)
				continue;
			var substitute = original.SubstituteTypeParameters(target);
			substitute = substitute.SubstituteTypeParameters(context);
			if (substitute != original)
			{
				newArguments[i] = new SymbolReference(substitute);
				constructNew = true;
			}
		}
		if (!constructNew)
			return target;
		return ConstructType(newArguments);
	}

	public override List<SymbolReference> Interfaces()
	{
		if (interfaces == null)
			BaseType();
		return interfaces;
	}

	public override TypeDefinitionBase BaseType()
	{
		if (baseType != null && !baseType.definition.IsValid() ||
			interfaces != null && interfaces.Exists(x => !x.definition.IsValid()))
		{
			baseType = null;
			interfaces = null;
		}

		if (interfaces == null)
		{
			var baseTypeDef = genericTypeDefinition.BaseType();
			baseType = baseTypeDef != null ? new SymbolReference(baseTypeDef.SubstituteTypeParameters(this)) : null;

			interfaces = new List<SymbolReference>(genericTypeDefinition.Interfaces());
			for (var i = 0; i < interfaces.Count; ++i)
				interfaces[i] = new SymbolReference(((TypeDefinitionBase) interfaces[i].definition).SubstituteTypeParameters(this));
		}
		return baseType != null ? baseType.definition as TypeDefinitionBase : null;
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return genericTypeDefinition.GetParameters();
	}

	public override bool CanConvertTo(TypeDefinitionBase otherType)
	{
		return genericTypeDefinition == otherType || DerivesFrom(otherType);
	}

	public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
	{
		if (genericTypeDefinition == otherType)
		{
			otherType = this;
			return true;
		}

		BaseType();

		if (otherType.kind == SymbolKind.Interface)
		{
			foreach (var i in interfaces)
				if (((TypeDefinitionBase) i.definition).DerivesFromRef(ref otherType))
				{
					otherType = otherType.SubstituteTypeParameters(this);
					return true;
				}
		}

		if (BaseType().DerivesFromRef(ref otherType))
		{
			otherType = otherType.SubstituteTypeParameters(this);
			return true;
		}

		return false;
	}

	public override bool DerivesFrom(TypeDefinitionBase otherType)
	{
		return genericTypeDefinition.DerivesFrom(otherType);
	}

	public override string GetName()
	{
		if (typeArguments == null || typeArguments.Length == 0)
			return name;

		var sb = new StringBuilder();
		sb.Append(name);
		var comma = "<";
		for (var i = 0; i < typeArguments.Length; ++i)
		{
			sb.Append(comma);
			if (typeArguments[i] != null)
				sb.Append(typeArguments[i].definition.GetName());
			comma = ", ";
		}
		sb.Append('>');
		return sb.ToString();
	}
	
	//public override string GetDelegateInfoText()
	//{
	//	var result = genericTypeDefinition.GetTooltipText();
	//	return result;
	//}

//	public override string GetTooltipText()
//	{
//		return base.GetTooltipText();

////		if (tooltipText != null)
////			return tooltipText;

//		if (parentSymbol != null && !string.IsNullOrEmpty(parentSymbol.GetName()))
//			tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSymbol.GetName() + ".";// + name;
//		else
//			tooltipText = kind.ToString().ToLowerInvariant() + " ";// +name;

//		tooltipText += GetName();
//		//tooltipText += "<" + (typeArguments[0] != null ? typeArguments[0].definition : genericTypeDefinition.typeParameters[0]).GetName();
//		//for (var i = 1; i < typeArguments.Length; ++i)
//		//    tooltipText += ", " + (typeArguments[i] != null ? typeArguments[i].definition : genericTypeDefinition.typeParameters[i]).GetName();
//		//tooltipText += '>';

//		var xmlDocs = GetXmlDocs();
//		if (!string.IsNullOrEmpty(xmlDocs))
//		{
//		    tooltipText += "\n\n" + xmlDocs;
//		}

//		return tooltipText;
//	}

	public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName = DecodeId(memberName);
		
		return genericTypeDefinition.FindName(memberName, numTypeParameters, asTypeOnly);
	}

	public Dictionary<SymbolDefinition, SymbolDefinition> constructedMembers;

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		genericTypeDefinition.ResolveMember(leaf, context, numTypeArgs);
		
		var genericMember = leaf.resolvedSymbol;
		if (genericMember == null)// || genericMember is MethodGroupDefinition)// !genericMember.IsGeneric)
			return;

		SymbolDefinition constructed;
		if (constructedMembers != null && constructedMembers.TryGetValue(genericMember, out constructed))
			leaf.resolvedSymbol = constructed;
		else
			leaf.resolvedSymbol = GetConstructedMember(genericMember);
	}

	public SymbolDefinition GetConstructedMember(SymbolDefinition member)
	{
		var parent = member.parentSymbol;
		if (parent is MethodGroupDefinition)
			parent = parent.parentSymbol;

		if (genericTypeDefinition != parent)
		{
		//	UnityEngine.Debug.Log(member.GetTooltipText() + " is not member of " + genericTypeDefinition.GetTooltipText());
			return member;
		}

		//if (!member.IsGeneric)
		//    return member;

		SymbolDefinition constructed;
		if (constructedMembers == null)
			constructedMembers = new Dictionary<SymbolDefinition, SymbolDefinition>();
		else if (constructedMembers.TryGetValue(member, out constructed))
			return constructed;

		constructed = ConstructMember(member);
		constructedMembers[member] = constructed;
		return constructed;
	}

	private SymbolDefinition ConstructMember(SymbolDefinition member)
	{
		SymbolDefinition symbol;
		if (member is InstanceDefinition)
		{
			symbol = new ConstructedInstanceDefinition(member as InstanceDefinition);
		}
		if (member is TypeDefinition)
		{
			symbol = (member as TypeDefinition).ConstructType(null);// new ConstructedTypeDefinition(member as TypeDefinition, null);
		}
		else
		{
			symbol = new ConstructedSymbolDefinition(member);
		}
		symbol.parentSymbol = this;
		return symbol;
	}

	public override bool IsSameType(TypeDefinitionBase type)
	{
		if (type == this)
			return true;
		
		var constructedType = type as ConstructedTypeDefinition;
		if (constructedType == null)
			return false;
		
		if (genericTypeDefinition != constructedType.genericTypeDefinition)
			return false;
		
		for (var i = 0; i < typeArguments.Length; ++i)
			if (!typeArguments[i].definition.IsSameType(constructedType.typeArguments[i].definition as TypeDefinitionBase))
				return false;
		
		return true;
	}

	protected override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		var indexers = GetAllIndexers();

		// TODO: Resolve overloads

		return indexers != null ? indexers[indexers.Count - 1] : null;
	}

	public override List<SymbolDefinition> GetAllIndexers()
	{
		List<SymbolDefinition> indexers = genericTypeDefinition.GetAllIndexers();
		if (indexers != null)
		{
			for (var i = 0; i < indexers.Count; ++i)
			{
				var member = indexers[i];
				member = GetConstructedMember(member);
				indexers[i] = member;
			}
		}
		return indexers;
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return false;
	//	}
	//}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		var dataFromDefinition = new Dictionary<string,SymbolDefinition>();
		genericTypeDefinition.GetMembersCompletionData(dataFromDefinition, flags, mask, assembly);
		foreach (var entry in dataFromDefinition)
		{
			if (!data.ContainsKey(entry.Key))
			{
				var member = GetConstructedMember(entry.Value);
				data.Add(entry.Key, member);
			}
		}

	//	base.GetMembersCompletionData(data, flags, mask, assembly);

		// TODO: Is this really needed?
	//	if (BaseType() != null && (kind != SymbolKind.Enum || flags != BindingFlags.Static))
	//		BaseType().GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, assembly);
	}
}

public class ConstructedSymbolDefinition : SymbolDefinition
{
	public readonly SymbolDefinition genericSymbol;

	public ConstructedSymbolDefinition(SymbolDefinition genericSymbolDefinition)
	{
		genericSymbol = genericSymbolDefinition;
		kind = genericSymbol.kind;
		modifiers = genericSymbol.modifiers;
		accessLevel = genericSymbol.accessLevel;
		name = genericSymbol.name;
	}

	public override SymbolDefinition TypeOf()
	{
		var result = genericSymbol.TypeOf() as TypeDefinitionBase;
		
		var ctx = parentSymbol as ConstructedTypeDefinition;
		if (ctx != null && result != null)
			result = result.SubstituteTypeParameters(ctx); 

		return result;
	}
	
	public override SymbolDefinition GetGenericSymbol()
	{
		return genericSymbol;
	}

	public override List<ParameterDefinition> GetParameters()
	{
		return genericSymbol.GetParameters();
	}

	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return genericSymbol.GetTypeParameters();
	}

	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		var symbolType = TypeOf() as TypeDefinitionBase;
		if (symbolType != null)
			symbolType.ResolveMember(leaf, context, numTypeArgs);
	}

	public SymbolDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, Scope scope)
	{
		if (kind != SymbolKind.MethodGroup)
			return null;
		var genericMethod = ((MethodGroupDefinition) genericSymbol).ResolveMethodOverloads(argumentListNode, scope);
		if (genericMethod == null || genericMethod.kind != SymbolKind.Method)
			return null;
		return ((ConstructedTypeDefinition) parentSymbol).GetConstructedMember(genericMethod);
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		var symbolType = TypeOf();
		if (symbolType != null)
			symbolType.GetMembersCompletionData(data, BindingFlags.Instance, mask, assembly);
	}
}

public class ArrayTypeDefinition : TypeDefinitionBase
{
	private static TypeDefinitionBase systemArrayType;

	public readonly TypeDefinitionBase elementType;
	public readonly int rank;

	public ArrayTypeDefinition(TypeDefinitionBase elementType, int rank)
	{
		kind = SymbolKind.Class;
		this.elementType = elementType;
		this.rank = rank;
		name = elementType.GetName() + RankString();
	}

	public override TypeDefinitionBase BaseType()
	{
		if (systemArrayType == null)
		{
			var assemblyDefinition = AssemblyDefinition.FromAssembly(typeof(System.Array).Assembly);
			systemArrayType = (TypeDefinitionBase) assemblyDefinition.FindNamespace("System").FindName("Array", 0, true);
		}
		return systemArrayType;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(ConstructedTypeDefinition context)
	{
		var constructedElement = elementType.SubstituteTypeParameters(context);
		if (constructedElement != elementType)
			return constructedElement.MakeArrayType(rank);

		return base.SubstituteTypeParameters(context);
	}

	protected override string RankString()
	{
		return '[' + new string(',', rank - 1) + ']';
	}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters, bool asTypeOnly)
	{
		symbolName = DecodeId(symbolName);
		
		var result = base.FindName(symbolName, numTypeParameters, asTypeOnly);
//		if (result == null && BaseType() != null)
//		{
//			//	Debug.Log("Symbol lookup '" + symbolName +"' in base " + baseType.definition);
//			result = BaseType().FindName(symbolName, numTypeParameters, asTypeOnly);
//		}
		return result;
	}

	public override string GetTooltipText()
	{
//		if (tooltipText != null)
//			return tooltipText;

		if (elementType == null)
			return "array of unknown type";

		if (parentSymbol != null && !string.IsNullOrEmpty(parentSymbol.GetName()))
			tooltipText = parentSymbol.GetName() + "." + elementType.GetName() + RankString();
		tooltipText = elementType.GetName() + RankString();

		var xmlDocs = GetXmlDocs();
		if (!string.IsNullOrEmpty(xmlDocs))
		{
			tooltipText += "\n\n" + xmlDocs;
		}

		return tooltipText;
	}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return elementType.IsGeneric;
	//	}
	//}
}

public class NullableTypeDefinition : TypeDefinitionBase
{
	private static TypeDefinitionBase systemNullableType;

	public readonly TypeDefinitionBase elementType;

	public NullableTypeDefinition(TypeDefinitionBase elementType)
	{
		kind = SymbolKind.Class;
		this.elementType = elementType;
		name = elementType.GetName() + "?";
	}

	public override TypeDefinitionBase BaseType()
	{
		if (systemNullableType == null)
		{
			var assemblyDefinition = AssemblyDefinition.FromAssembly(typeof(System.Nullable).Assembly);
			systemNullableType = (TypeDefinitionBase) assemblyDefinition.FindNamespace("System").FindName("Nullable", 1, true);
		}
		return systemNullableType;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(ConstructedTypeDefinition context)
	{
		var constructedElement = elementType.SubstituteTypeParameters(context);
		if (constructedElement != elementType)
			return constructedElement.MakeNullableType();

		return base.SubstituteTypeParameters(context);
	}

	public override string GetTooltipText()
	{
		//if (tooltipText == null)
		{
			if (parentSymbol != null && !string.IsNullOrEmpty(parentSymbol.GetName()))
				tooltipText = parentSymbol.GetName() + "." + elementType.GetName() + "?";
			tooltipText = elementType.GetName() + "?";
			
			var xmlDocs = GetXmlDocs();
			if (!string.IsNullOrEmpty(xmlDocs))
			{
				tooltipText += "\n\n" + xmlDocs;
			}
		}

		return tooltipText;
	}
}

public class TypeDefinition : TypeDefinitionBase
{
	protected SymbolReference baseType;
	protected List<SymbolReference> interfaces;
	
	public List<TypeParameterDefinition> typeParameters;
	//public SymbolReference[] tempTypeArguments;

	private Dictionary<string, ConstructedTypeDefinition> constructedTypes;
	public virtual ConstructedTypeDefinition ConstructType(SymbolReference[] typeArgs)
	{
		var delimiter = string.Empty;
		var sb = new StringBuilder();
		if (typeArgs != null)
		{
			foreach (var arg in typeArgs)
			{
				sb.Append(delimiter);
				sb.Append(arg.ToString());
				delimiter = ", ";
			}
		}
		var sig = sb.ToString();

		if (constructedTypes == null)
			constructedTypes = new Dictionary<string, ConstructedTypeDefinition>();

		ConstructedTypeDefinition result;
//		if (constructedTypes.TryGetValue(sig, out result))
//		{
//			if (result.IsValid())
//			{
//				return result;
//			}
//		}

		result = new ConstructedTypeDefinition(this, typeArgs);
		constructedTypes[sig] = result;
		return result;
	}

	public override SymbolDefinition TypeOf()
	{
		return this;
	}

	public override List<SymbolReference> Interfaces()
	{
		if (interfaces == null)
			BaseType();
		return interfaces;
	}

	public override TypeDefinitionBase BaseType()
	{
		if (baseType != null && (baseType.definition == null || !baseType.definition.IsValid()) ||
			interfaces != null && interfaces.Exists(x => !x.definition.IsValid()))
		{
			baseType = null;
			interfaces = null;
		}

		if (baseType == null && interfaces == null)
		{
			interfaces = new List<SymbolReference>();

			var decl = declarations != null ? declarations.FirstOrDefault() : null;
			if (decl != null)
			{
				var baseNode = (ParseTree.Node) decl.parseTreeNode.FindChildByName(
					decl.kind == SymbolKind.Class ? "classBase" :
					decl.kind == SymbolKind.Struct ? "structInterfaces" :
					"interfaceBase");
				var interfaceListNode = baseNode != null ? baseNode.NodeAt(1) : null;

				switch (decl.kind)
				{
					case SymbolKind.Class:
						if (interfaceListNode != null)
						{
							baseType = new SymbolReference(interfaceListNode.ChildAt(0));
							if (baseType.definition.kind == SymbolKind.Interface)
							{
								interfaces.Add(baseType);
								baseType = ReflectedTypeReference.ForType(typeof(object));
							}

							for (var i = 2; i < interfaceListNode.numValidNodes; i += 2)
								interfaces.Add(new SymbolReference(interfaceListNode.ChildAt(i)));
						}
						else
						{
							baseType = ReflectedTypeReference.ForType(typeof(object));
						}
						break;

					case SymbolKind.Struct:
					case SymbolKind.Interface:
						baseType = decl.kind == SymbolKind.Struct ? ReflectedTypeReference.ForType(typeof(ValueType)) : null;
						if (baseNode != null && baseNode.numValidNodes >= 2)
						{
							interfaceListNode = baseNode.NodeAt(1);
							for (var i = 0; i < interfaceListNode.numValidNodes; i += 2)
								interfaces.Add(new SymbolReference(interfaceListNode.ChildAt(i)));
						}
						break;

					case SymbolKind.Enum:
						baseType = ReflectedTypeReference.ForType(typeof(Enum));
						break;

					case SymbolKind.Delegate:
						baseType = ReflectedTypeReference.ForType(typeof(MulticastDelegate));
						break;
				}
			}
			//Debug.Log("BaseType() of " + this + " is " + (baseType != null ? baseType.definition.ToString() : "null"));
		}
		return baseType != null ? baseType.definition as TypeDefinitionBase : null;
	}

	public override bool DerivesFrom(TypeDefinitionBase otherType)
	{
		if (this == otherType)
			return true;

		if (interfaces == null)
			BaseType();
		if (interfaces != null)
			for (var i = 0; i < interfaces.Count; ++i)
			{
				var typeDefinition = interfaces[i].definition as TypeDefinitionBase;
				if (typeDefinition != null && typeDefinition.DerivesFrom(otherType))
					return true;
			}

		if (BaseType() != null)
			return BaseType().DerivesFrom(otherType);
		
		return false;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind != SymbolKind.TypeParameter)
			return base.AddDeclaration(symbol);

		var oldReflectionName = ReflectionName;
		
		var symbolName = symbol.ReflectionName;// symbol.Name;
		if (typeParameters == null)
			typeParameters = new List<TypeParameterDefinition>();
		var definition = typeParameters.Find(x => x.name == symbolName);
		if (definition == null)
		{
			definition = (TypeParameterDefinition) Create(symbol);
			definition.parentSymbol = this;
			typeParameters.Add(definition);
		}

		symbol.definition = definition;

		var nameNode = symbol.NameNode();
		if (nameNode != null)
		{
			var leaf = nameNode as ParseTree.Leaf;
			if (leaf != null)
				leaf.SetDeclaredSymbol(definition);
			else
			{
				var lastLeaf = ((ParseTree.Node) nameNode).GetLastLeaf();
				if (lastLeaf != null)
				{
					if (lastLeaf.parent.RuleName == "typeParameterList")
						lastLeaf = lastLeaf.parent.parent.LeafAt(0);
					lastLeaf.SetDeclaredSymbol(definition);
				}
			}
		}
		
		if (parentSymbol.members.Remove(oldReflectionName))
			parentSymbol.members[ReflectionName] = this;

		return definition;
	}

	public override void RemoveDeclaration (SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.TypeParameter && typeParameters != null)
		{
			var oldReflectionName = ReflectionName;
			if (typeParameters.Remove(symbol.definition as TypeParameterDefinition))
			{
				parentSymbol.members.Remove(oldReflectionName);
				parentSymbol.members[ReflectionName] = this;
			}
		}

		base.RemoveDeclaration (symbol);
	}

	public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName = DecodeId(memberName);
		
		if (numTypeParameters == 0 && typeParameters != null)
		{
			var definition = typeParameters.Find(x => x.name == memberName);
			if (definition != null)
				return definition;
		}
		var member = base.FindName(memberName, numTypeParameters, asTypeOnly);
//		if (member != null)
			return member;
//		if (BaseType() == null)
//			return null;
//		var rt = BaseType() as ReflectedType;
//		if (rt != null)
//			rt.ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
//		return BaseType() != null ? BaseType().FindName(memberName, numTypeParameters, asTypeOnly) : null;
	}

	//public List<SymbolDefinition> MemberLookup(string memberName, Scope context)
	//{
	//    var accessLevelMask = AccessLevelMask.Public;
	//    var contextType = context.EnclosingType();
	//    if (contextType != null)
	//    {
	//        if (contextType == this)
	//            accessLevelMask = AccessLevelMask.Public | AccessLevelMask.Internal | AccessLevelMask.Protected | AccessLevelMask.Private;
	//        else if (IsSameOrParentOf(contextType))
	//            accessLevelMask = AccessLevelMask.Public | AccessLevelMask.Internal | AccessLevelMask.Protected;
	//        else
	//            accessLevelMask = AccessLevelMask.Public;
	//    }

	//    List<SymbolDefinition> candidates = null;

	//    SymbolDefinition member;
	//    if (members.TryGetValue(memberName, out member))
	//    {
	//        if (member.IsAccessible(accessLevelMask))
	//    //	if ((access & (member.modifiers | Modifiers.Private)) != 0 && (member.modifiers & Modifiers.Override) == 0)
	//            candidates = new List<SymbolDefinition> { member };
	//    }

	//    return candidates;
	//}

	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return typeParameters;
	}

	public override string GetTooltipText()
	{
		if (kind == SymbolKind.Delegate)
			return base.GetTooltipText();

	//	if (tooltipText != null)
	//		return tooltipText;

		var parentSD = parentSymbol;
		if (parentSD != null && !string.IsNullOrEmpty(parentSD.GetName()))
			tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSD.GetName() + "." + name;
		else
			tooltipText = kind.ToString().ToLowerInvariant() + " " + name;

		if (typeParameters != null)
		{
			tooltipText += "<" + TypeOfTypeParameter(typeParameters[0]).GetName();
			for (var i = 1; i < typeParameters.Count; ++i)
				tooltipText += ", " + TypeOfTypeParameter(typeParameters[i]).GetName();
			tooltipText += '>';
		}

		var xmlDocs = GetXmlDocs();
		if (!string.IsNullOrEmpty(xmlDocs))
		{
		    tooltipText += "\n\n" + xmlDocs;
		}

		return tooltipText;
	}

	public override TypeDefinitionBase SubstituteTypeParameters(ConstructedTypeDefinition context)
	{
		if (typeParameters == null)
			return base.SubstituteTypeParameters(context);
		
		var constructType = false;
		var typeArguments = new SymbolReference[typeParameters.Count];
		for (var i = 0; i < typeArguments.Length; ++i)
		{
			typeArguments[i] = new SymbolReference(typeParameters[i]);
			var original = typeParameters[i];
			if (original == null)
				continue;
			var substitute = original.SubstituteTypeParameters(context);
			if (substitute != original)
			{
				typeArguments[i] = new SymbolReference(substitute);
				constructType = true;
			}
		}
		if (!constructType)
			return this;
		return ConstructType(typeArguments);
	}


	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		return typeParameters != null;
	//	}
	//}
}

public class MethodGroupDefinition : SymbolDefinition
{
	private static readonly MethodDefinition ambiguousMethodOverload = new MethodDefinition { kind = SymbolKind.Error, name = "ambiguous method overload" };
	private static readonly MethodDefinition unresolvedMethodOverload = new MethodDefinition { kind = SymbolKind.Error, name = "unresolved method overload" };

	public HashSet<MethodDefinition> methods = new HashSet<MethodDefinition>();

	public void AddMethod(MethodDefinition method)
	{
		methods.RemoveWhere((MethodDefinition x) => !x.IsValid());
		if (method.declarations != null)
		{
			var d = method.declarations[0];
			methods.RemoveWhere((MethodDefinition x) => x.declarations != null && x.declarations.Contains(d));
		}
		methods.Add(method);
		method.parentSymbol = this;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		//Debug.Log("removing " + symbol.Name + " - " + (symbol.parseTreeNode.ChildAt(0) ?? symbol.parseTreeNode).Print());
		methods.RemoveWhere((MethodDefinition x) => x.declarations.Contains(symbol));
	}

	public SymbolDefinition ResolveParameterName(ParseTree.Leaf leaf)
	{
		foreach (var m in methods)
		{
			var p = m.GetParameters();
			var x = p.Find(pd => pd.name == leaf.token.text);
			if (x != null)
				return leaf.resolvedSymbol = x;
		}
		return unknownSymbol;
	}

	public MethodDefinition ResolveMethodOverloads(ParseTree.Node argumentListNode, Scope scope)
	{
		if (argumentListNode == null)
			return methods.FirstOrDefault();
		var argumentTypes = new List<TypeDefinitionBase>();
		for (var i = 0; i < argumentListNode.numValidNodes; i += 2)
		{
			var argType = ResolveNode(argumentListNode.ChildAt(i));
			if (argType != null)
				argType = argType.TypeOf();
			argumentTypes.Add(argType as TypeDefinitionBase);
		}
		var resolved = ResolveMethodOverloads(argumentTypes, scope);
		return resolved;
	}

	public MethodDefinition ResolveMethodOverloads(List<TypeDefinitionBase> argumentTypes, Scope scope)
	{
		var candidates = new List<MethodDefinition>();
		foreach (var method in methods)
			if (!method.IsOverride && method.CanCallWithNArguments(argumentTypes.Count))
				candidates.Add(method);

		if (candidates.Count == 0)
		{
			var baseType = (TypeDefinitionBase) parentSymbol;
			while ((baseType = baseType.BaseType()) != null)
			{
				var baseSymbol = baseType.FindName(name, 0, false) as MethodGroupDefinition; // FIXME: <== 0
				if (baseSymbol != null)
					return baseSymbol.ResolveMethodOverloads(argumentTypes, scope);
			}
			return unresolvedMethodOverload;
		}

		if (candidates.Count == 1)
			return candidates[0];

		// find best match
		MethodDefinition bestMatch = null;
		var bestExactMatches = -1;
		foreach (var method in candidates)
		{
			var parameters = method.GetParameters();
			var exactMatches = 0;
			ParameterDefinition paramsArray = null;
			for (var i = 0; i < argumentTypes.Count; ++i)
			{
				if (argumentTypes[i] == null)
				{
					exactMatches = -1;
					break;
				}
				
				if (paramsArray == null && parameters[i].IsParametersArray)
					paramsArray = parameters[i];
					
				TypeDefinitionBase parameterType = null;
				if (paramsArray != null)
				{
					var arrayType = paramsArray.TypeOf() as ArrayTypeDefinition;
					if (arrayType != null)
						parameterType = arrayType.elementType;
				}
				else
				{
					parameterType = parameters[i].TypeOf() as TypeDefinitionBase;
				}
				if (argumentTypes[i].IsSameType(parameterType))
				{
					++exactMatches;
					continue;
				}
				if (!argumentTypes[i].CanConvertTo(parameterType))
				{
					exactMatches = -1;
					break;
				}
			}
			if (exactMatches < 0)
				continue;
			if (exactMatches > bestExactMatches)
			{
				bestExactMatches = exactMatches;
				bestMatch = method;
			}
		}

		return bestMatch ?? ambiguousMethodOverload;
	}

	public override bool IsAccessible(AccessLevelMask accessLevelMask)
	{
		foreach (var kv in members)
			if (kv.Value.IsAccessible(accessLevelMask))
				return true;
		return false;
	}
}

public abstract class InvokeableSymbolDefinition : SymbolDefinition
{
	public abstract TypeDefinitionBase ReturnType();

	protected SymbolReference returnType;
	public List<ParameterDefinition> parameters;
	public List<TypeParameterDefinition> typeParameters;

	public bool CanCallWithNArguments(int numArguments)
	{
		var minArgs = 0;
		var maxArgs = 0;
		if (parameters != null)
			foreach (var param in parameters)
				if (!param.IsThisParameter)
				{
					if (param.IsParametersArray)
						maxArgs = 100000;
					else if (!param.IsOptional)
						++minArgs;
					++maxArgs;
				}
		if (numArguments < minArgs || numArguments > maxArgs)
			return false;

		return true;
	}

	public override SymbolDefinition TypeOf()
	{
		return ReturnType();
	}
	
	public override List<ParameterDefinition> GetParameters()
	{
		return parameters ?? _emptyParameterList;
	}

	public override List<TypeParameterDefinition> GetTypeParameters()
	{
		return typeParameters;
	}

	public SymbolDefinition AddParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var parameter = (ParameterDefinition) Create(symbol);
		parameter.type = new SymbolReference(symbol.parseTreeNode.FindChildByName("type"));
		parameter.parentSymbol = this;
		var lastNode = symbol.parseTreeNode.NodeAt(-1);
		if (lastNode != null && lastNode.RuleName == "defaultArgument")
		{
			var defaultValueNode = lastNode.NodeAt(1);
			if (defaultValueNode != null)
				parameter.defaultValue = defaultValueNode.Print();
		}
		if (!string.IsNullOrEmpty(symbolName))
		{
			if (parameters == null)
				parameters = new List<ParameterDefinition>();
			parameters.Add(parameter);
		}
		return parameter;
	}
	
	public SymbolDefinition AddTypeParameter(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		if (typeParameters == null)
			typeParameters = new List<TypeParameterDefinition>();
		var definition = typeParameters.Find(x => x.name == symbolName);
		if (definition == null)
		{
			definition = (TypeParameterDefinition) Create(symbol);
			definition.parentSymbol = this;
			typeParameters.Add(definition);
		}

		symbol.definition = definition;

		var nameNode = symbol.NameNode();
		if (nameNode != null)
		{
			var leaf = nameNode as ParseTree.Leaf;
			if (leaf != null)
				leaf.SetDeclaredSymbol(definition);
			else
			{
				var lastLeaf = ((ParseTree.Node) nameNode).GetLastLeaf();
				if (lastLeaf != null)
				{
					if (lastLeaf.parent.RuleName == "typeParameterList")
						lastLeaf = lastLeaf.parent.parent.LeafAt(0);
					lastLeaf.SetDeclaredSymbol(definition);
				}
			}
		}

		return definition;
	}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter)
		{
			SymbolDefinition definition = AddParameter(symbol);
			//	if (!members.TryGetValue(symbolName, out definition) || definition is ReflectedMember || definition is ReflectedType)
			//		definition = AddMember(symbol);

			symbol.definition = definition;
			return definition;
		}
		else if (symbol.kind == SymbolKind.TypeParameter)
		{
			SymbolDefinition definition = AddTypeParameter(symbol);
			return definition;
		}

		return base.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Parameter && parameters != null)
			parameters.Remove((ParameterDefinition) symbol.definition);
		else if (symbol.kind == SymbolKind.TypeParameter && typeParameters != null)
			typeParameters.Remove((TypeParameterDefinition) symbol.definition);
		else
			base.RemoveDeclaration(symbol);
	}

	public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName = DecodeId(memberName);
		
		if (!asTypeOnly && numTypeParameters == 0 && parameters != null)
		{
			var definition = parameters.Find(x => x.name == memberName);
			if (definition != null)
				return definition;
		}
		else if (typeParameters != null)
		{
			var definition = typeParameters.Find(x => x.name == memberName);
			if (definition != null)
				return definition;
		}
		return base.FindName(memberName, numTypeParameters, asTypeOnly);
	}
	
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		if (parameters != null)
		{
			var leafText = DecodeId(leaf.token.text);
			var definition = parameters.Find(x => x.name == leafText);
			if (definition != null)
			{
				leaf.resolvedSymbol = definition;
				return;
			}
		}
		base.ResolveMember(leaf, context, numTypeArgs);
	}

	//public override string GetTooltipText()
	//{
	//    if (tooltipText != null)
	//        return tooltipText;

	//    var parentSD = parentSymbol;
	//    if (parentSD != null && !string.IsNullOrEmpty(parentSD.GetName()))
	//        tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSD.GetName() + "." + name;
	//    else
	//        tooltipText = kind.ToString().ToLowerInvariant() + " " + name;

	//    var typeOf = TypeOf();
	//    var typeName = "";
	//    if (typeOf != null && kind != SymbolKind.Constructor && kind != SymbolKind.Destructor)
	//    {
	//        //var tp = typeOf as TypeParameterDefinition;
	//        //if (tp != null)
	//        //    typeOf = TypeOfTypeParameter(tp);
	//        var ctx = parentSymbol as ConstructedTypeDefinition;
	//        if (ctx != null)
	//            typeOf = ((TypeDefinitionBase) typeOf).SubstituteTypeParameters(ctx);
	//        typeName = typeOf.GetName() + " ";

	//        if (typeOf.kind != SymbolKind.TypeParameter)
	//            for (var parentType = typeOf.parentSymbol as TypeDefinitionBase; parentType != null; parentType = parentType.parentSymbol as TypeDefinitionBase)
	//                typeName = parentType.GetName() + '.' + typeName;
	//    }

	//    var parentText = string.Empty;
	//    var parent = parentSymbol is MethodGroupDefinition ? parentSymbol.parentSymbol : parentSymbol;
	//    if ((parent is TypeDefinitionBase && parent.kind != SymbolKind.Delegate && kind != SymbolKind.TypeParameter)
	//        || parent is NamespaceDefinition
	//        )//|| kind == SymbolKind.Accessor)
	//    {
	//        var parentName = parent.GetName();
	//        if (kind == SymbolKind.Constructor)
	//        {
	//            var typeParent = parent.parentSymbol as TypeDefinitionBase;
	//            parentName = typeParent != null ? typeParent.GetName() : null;
	//        }
	//        if (!string.IsNullOrEmpty(parentName))
	//            parentText = parentName + ".";
	//    }

	//    var nameText = name;

	//    List<ParameterDefinition> parameters = GetParameters();
	//    var parametersText = string.Empty;
	//    string parametersEnd = null;

	//    if (kind == SymbolKind.Method)
	//    {
	//        nameText += '(';
	//        //parameters = ((MethodDefinition) this).parameters;
	//        parametersEnd = ")";
	//    }
	//    else if (kind == SymbolKind.Constructor)
	//    {
	//        nameText = parent.name + '(';
	//        //parameters = ((MethodDefinition) this).parameters;
	//        parametersEnd = ")";
	//    }
	//    else if (kind == SymbolKind.Destructor)
	//    {
	//        nameText = "~" + parent.name + "()";
	//    }
	//    else if (kind == SymbolKind.Indexer)
	//    {
	//        nameText = "this[";
	//        //parameters = ((IndexerDefinition) this).parameters;
	//        parametersEnd = "]";
	//    }
	//    else if (kind == SymbolKind.Delegate)
	//    {
	//        nameText += '(';
	//        //parameters = ((DelegateTypeDefinition) this).parameters;
	//        parametersEnd = ")";
	//    }

	//    if (parameters != null)
	//    {
	//        parametersText = PrintParameters(parameters);
	//    }

	//    tooltipText = kindText + typeName + parentText + nameText + parametersText + parametersEnd;

	//    if (typeOf != null && typeOf.kind == SymbolKind.Delegate)
	//    {
	//        tooltipText += "\n\nDelegate info\n";
	//        tooltipText += typeOf.GetDelegateInfoText();
	//    }

	//    return tooltipText;
	//}

	//public override bool IsGeneric
	//{
	//	get
	//	{
	//		if (ReturnType().IsGeneric)
	//			return true;
	//		var numParams = parameters == null ? 0 : parameters.Count;
	//		for (var i = 0; i < numParams; ++i)
	//			if (parameters[i].TypeOf().IsGeneric)
	//				return true;
	//		return false;
	//	}
	//}
}

public class MethodDefinition : InvokeableSymbolDefinition//, IComparable<MethodDefinition>
{
	public NamespaceDefinition NamespaceOfExtensionMethod;

	public MethodDefinition()
	{
		kind = SymbolKind.Method;
	}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		var result = base.AddDeclaration(symbol);
		if (result.kind == SymbolKind.Parameter && (result.modifiers & Modifiers.This) != 0)
		{
			var parentType = (parentSymbol.kind == SymbolKind.MethodGroup ? parentSymbol.parentSymbol : parentSymbol) as TypeDefinitionBase;
			//Debug.Log(this + " is extension method declared in " + parentType.FullName);

			var namespaceDefinition = parentType.parentSymbol;
			while (!(namespaceDefinition is NamespaceDefinition))
				namespaceDefinition = namespaceDefinition.parentSymbol;
			NamespaceOfExtensionMethod = namespaceDefinition as NamespaceDefinition;
		}
		
		return result;
	}
	
//	public override void RemoveDeclaration(SymbolDeclaration symbol)
//	{
//		base.RemoveDeclaration(symbol);
//	}

	public override TypeDefinitionBase ReturnType()
	{
		if (returnType == null)
		{
			if (kind == SymbolKind.Constructor)
				return parentSymbol as TypeDefinitionBase ?? unknownType;

			if (declarations != null)
			{
				ParseTree.BaseNode refNode = null;
				switch (declarations[0].parseTreeNode.RuleName)
				{
					case "methodDeclaration":
					case "interfaceMethodDeclaration":
						refNode = declarations[0].parseTreeNode.FindPreviousNode();
						break;
					default:
						refNode = declarations[0].parseTreeNode.parent.parent.ChildAt(declarations[0].parseTreeNode.parent.childIndex - 1);
						break;
				}
				if (refNode == null)
					Debug.LogError("Could not find method return type from node: " + declarations[0].parseTreeNode);
				returnType = refNode != null ? new SymbolReference(refNode) : null;
			}
		}
		
		return returnType == null ? unknownType : returnType.definition as TypeDefinitionBase ?? unknownType;
	}

	public override void GetMembersCompletionData (Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		foreach (var parameter in GetParameters())
		{
			var parameterName = parameter.GetName();
			if (!data.ContainsKey(parameterName))
				data.Add(parameterName, parameter);
		}
		if ((flags & (BindingFlags.Instance | BindingFlags.Static)) != BindingFlags.Instance)
		{
			var tp = GetTypeParameters();
			if (tp != null)
				foreach (var parameter in GetTypeParameters())
				{
					var parameterName = parameter.name;
					if (!data.ContainsKey(parameterName))
						data.Add(parameterName, parameter);
				}
		}
	//	ReturnType().GetMembersCompletionData(data, flags, mask, assembly);
	}

//	public int CompareTo(MethodDefinition other)
//	{
//		var numTypeParams = typeParameters == null ? 0 : typeParameters.Count;
//		var numTypeParamsOther = other.typeParameters == null ? 0 : other.typeParameters.Count;
//		if (numTypeParams < numTypeParamsOther)
//			return -1;
//		if (numTypeParams > numTypeParamsOther)
//			return 1;
//
//		var numParams = parameters == null ? 0 : parameters.Count;
//		var numParamsOther = other.parameters == null ? 0 : other.parameters.Count;
//		if (numParams < numParamsOther)
//			return -1;
//		if (numParams > numParamsOther)
//			return 1;
//
//		for (var i = 0; i < numParams; ++i)
//		{
//			var paramType = parameters[i].TypeOf();
//			var paramTypeOther = other.parameters[i].TypeOf();
//			if (paramType != paramTypeOther)
//				return paramType.GetTooltipText().CompareTo(paramTypeOther.GetTooltipText());
//		}
//		return 0;
//	}

	public bool IsOverride
	{
		get { return (modifiers & Modifiers.Override) != 0; }
		set { throw new InvalidOperationException(); }
	}
}

public class NamespaceDefinition : SymbolDefinition
{
//	public virtual SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
//	{
//		Debug.Log("Adding " + symbol + " to namespace " + name);
//		return base.AddDeclaration(symbol);
//	}
//	
//	public virtual void RemoveDeclaration(SymbolDeclaration symbol)
//	{
//		Debug.Log("Removing " + symbol + " from namespace " + name);
//		base.RemoveDeclaration(symbol);
//	}
	
	//public override SymbolDefinition FindName(string memberName)
	//{
	//    var result = base.FindName(memberName);
	//    if (result == null)
	//    {
	//        UnityEngine.Debug.Log(memberName + " not found in " + GetTooltipText());
	//    }
	//    return result;
	//}

	private bool resolvingMember = false;
	public override void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		if (resolvingMember)
			return;
		resolvingMember = true;
		
		leaf.resolvedSymbol = null;
		if (declarations != null)
		{
			foreach (var declaration in declarations)
			{
				declaration.scope.Resolve(leaf, numTypeArgs);
				if (leaf.resolvedSymbol != null)
				{
					resolvingMember = false;
					return;
				}
			}
		}
		
		resolvingMember = false;

		base.ResolveMember(leaf, context, numTypeArgs);
		
		if (leaf.resolvedSymbol == null)
		{
			if (context != null)
			{
				var assemblyDefinition = context.GetAssembly();
				//while (namespaceScope.parentScope != null)
				//	namespaceScope = (NamespaceScope) namespaceScope.parentScope;
				//var assemblyDefinition = ((CompilationUnitScope) namespaceScope).assembly;
				assemblyDefinition.ResolveInReferencedAssemblies(leaf, this, numTypeArgs);
			}
		}
	}

	public override void ResolveAttributeMember(ParseTree.Leaf leaf, Scope context)
	{
		leaf.resolvedSymbol = null;
		if (declarations != null)
		{
			foreach (var declaration in declarations)
			{
				declaration.scope.ResolveAttribute(leaf);
				if (leaf.resolvedSymbol != null)
					return;
			}
		}

		base.ResolveAttributeMember(leaf, context);
		if (leaf.resolvedSymbol == null)
		{
			var namespaceScope = context as NamespaceScope;
			if (namespaceScope != null)
			{
				var assemblyDefinition = namespaceScope.GetAssembly();
				//while (namespaceScope.parentScope != null)
				//	namespaceScope = (NamespaceScope) namespaceScope.parentScope;
				//var assemblyDefinition = ((CompilationUnitScope) namespaceScope).assembly;
				assemblyDefinition.ResolveAttributeInReferencedAssemblies(leaf, this);
			}
		}
	}
	
	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		GetMembersCompletionData(data, fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any, assembly);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		base.GetMembersCompletionData(data, flags, mask, assembly);

		var assemblyDefinition = assembly ?? parentSymbol;
		while (assemblyDefinition != null && !(assemblyDefinition is AssemblyDefinition))
			assemblyDefinition = assemblyDefinition.parentSymbol;
		((AssemblyDefinition) assemblyDefinition).GetMembersCompletionDataFromReferencedAssemblies(data, this);
	}

	//public override bool IsPublic
	//{
	//	get
	//	{
	//		return true;
	//	}
	//}

	public override TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		return tp;
	}

	public override string GetTooltipText()
	{
		return name == string.Empty ? "global namespace" : base.GetTooltipText();
	}

	public void GetExtensionMethodsCompletionData(TypeDefinitionBase targetType, Dictionary<string, SymbolDefinition> data, AccessLevelMask accessLevelMask)
	{
//	Debug.Log("Extensions for " + targetType.GetTooltipText());
 		foreach (var t in members)
		{
			if (t.Value.kind == SymbolKind.Class && t.Value.IsStatic)
			{
				var classMembers = t.Value.members;
				foreach (var cm in classMembers)
				{
					if (cm.Value.kind == SymbolKind.MethodGroup)
					{
						var mg = cm.Value as MethodGroupDefinition;
						if (mg == null)
							continue;
						foreach (var m in mg.methods)
						{
							if (m.kind != SymbolKind.Method)
								continue;
							if (!m.IsStatic)
								continue;
							if (m.NamespaceOfExtensionMethod == null)
								continue;
							Debug.Log(m.GetTooltipText() + " in " + m.NamespaceOfExtensionMethod);
						}
					}
					else if (cm.Value.kind == SymbolKind.Method)
					{
						var m = cm.Value as MethodDefinition;
						if (m == null)
							continue;
						if (!m.IsStatic)
							continue;
						if (m.NamespaceOfExtensionMethod == null)
							continue;
						Debug.Log(m.GetTooltipText() + " in " + m.NamespaceOfExtensionMethod);
					}
				}
			}
		}
	}
}

public class CompilationUnitDefinition : NamespaceDefinition
{
	
}

public class SymbolDeclarationScope : Scope
{
	public SymbolDeclaration declaration;
	
	public SymbolDeclarationScope(ParseTree.Node node) : base(node) {}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
	//	if (symbol.kind == SymbolKind.Method)// || symbol.kind == SymbolKind.LambdaExpression)
	//	{
	//		declaration = symbol;
	//		return parentScope.AddDeclaration(symbol);
	//	}
		symbol.scope = this;
		return declaration.definition.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if ((symbol.kind == SymbolKind.Method /*|| symbol.kind == SymbolKind.LambdaExpression*/) && declaration == symbol)
		{
			declaration = null;
			parentScope.RemoveDeclaration(symbol);
		}
		else if (declaration != null && declaration.definition != null)
		{
			declaration.definition.RemoveDeclaration(symbol);
		}
	}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		throw new NotImplementedException();
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly)
	{
		throw new InvalidOperationException();
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		if (declaration != null && declaration.definition != null)
			declaration.definition.ResolveMember(leaf, this, numTypeArgs);

		//if (leaf.resolvedSymbol == SymbolDefinition.unknownSymbol)
		//	UnityEngine.Debug.LogError(leaf);

		if (leaf.resolvedSymbol == null)
			base.Resolve(leaf, numTypeArgs);
	}

	public override void ResolveAttribute(ParseTree.Leaf leaf)
	{
		if (declaration != null)
			declaration.definition.ResolveAttributeMember(leaf, this);

		if (leaf.resolvedSymbol == null)
			base.ResolveAttribute(leaf);
	}

	public override TypeDefinition EnclosingType()
	{
		if (declaration != null)
		{
			switch (declaration.kind)
			{
				case SymbolKind.Class:
				case SymbolKind.Struct:
				case SymbolKind.Interface:
					return (TypeDefinition) declaration.definition;
			}
		}
		return parentScope != null ? parentScope.EnclosingType() : null;
	}
}

public class TypeBaseScope : Scope
{
	public TypeDefinitionBase definition;
	
	public TypeBaseScope(ParseTree.Node node) : base(node) {}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		//Debug.Log("Adding base types list: " + symbol);
		//if (definition != null)
		//    definition.baseType = new SymbolReference { identifier = symbol.Name };
		//Debug.Log("baseType: " + definition.baseType.definition);
		return null;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
	}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		return parentScope.parentScope.FindName(symbolName, numTypeParameters);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		if (parentScope != null && parentScope.parentScope != null)
			parentScope.parentScope.Resolve(leaf, numTypeArgs);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly)
	{
		parentScope.GetMembersCompletionData(data, flags, includePrivate, assembly);
	//	definition.GetMembersCompletionData(data, flags, includePrivate, assembly);
	}
}

public class BodyScope : LocalScope
{
	public SymbolDefinition definition;
	
	public BodyScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		symbol.scope = this;
	//	Debug.Log("Adding declaration " + symbol + " to " + definition);

		switch (symbol.kind)
		{
		case SymbolKind.ConstantField:
		case SymbolKind.LocalConstant:
			if (!(definition is TypeDefinitionBase))
				return base.AddDeclaration(symbol);
			break;
		case SymbolKind.Variable:
		case SymbolKind.ForEachVariable:
		case SymbolKind.FromClauseVariable:
			return base.AddDeclaration(symbol);
		}

		return definition.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		switch (symbol.kind)
		{
		case SymbolKind.ConstantField:
		case SymbolKind.LocalConstant:
		case SymbolKind.Variable:
		case SymbolKind.ForEachVariable:
		case SymbolKind.FromClauseVariable:
			base.RemoveDeclaration(symbol);
			return;
		}

		if (definition != null)
			definition.RemoveDeclaration(symbol);
		base.RemoveDeclaration(symbol);
	}

	//public virtual SymbolDefinition ImportReflectedType(Type type)
	//{
	//    throw new InvalidOperationException();
	//}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		return definition.FindName(symbolName, numTypeParameters, false);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		leaf.resolvedSymbol = null;

		if (definition != null)
			definition.ResolveMember(leaf, this, numTypeArgs);
		
		if (numTypeArgs == 0 && leaf.resolvedSymbol == null)
		{
			var typeParams = definition.GetTypeParameters();
			if (typeParams != null)
				leaf.resolvedSymbol = typeParams.Find(x => x.GetName() == SymbolDefinition.DecodeId(leaf.token.text));
		}

		//if (leaf.resolvedSymbol == SymbolDefinition.unknownSymbol)
		//	UnityEngine.Debug.LogError(leaf);

		if (leaf.resolvedSymbol != null)
			return;

		base.Resolve(leaf, numTypeArgs);
	}

	public override void ResolveAttribute(ParseTree.Leaf leaf)
	{
		leaf.resolvedSymbol = null;
		if (definition != null)
			definition.ResolveAttributeMember(leaf, this);

		if (leaf.resolvedSymbol == null)
			base.ResolveAttribute(leaf);
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		GetMembersCompletionData(data, BindingFlags.NonPublic | (fromInstance ? BindingFlags.Instance : 0) | BindingFlags.Static, true, assembly);
		if (fromInstance && definition != null && !definition.IsInstanceMember)
			fromInstance = false;
		base.GetCompletionData(data, fromInstance, assembly);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly)
	{
		definition.GetMembersCompletionData(data, flags, includePrivate ? AccessLevelMask.Any : AccessLevelMask.Public, assembly);
	}
}

public class NamespaceScope : Scope
{
	public NamespaceDeclaration declaration;
	public NamespaceDefinition definition;

	public List<SymbolDeclaration> typeDeclarations;

	public NamespaceScope(ParseTree.Node node) : base(node) {}
	
	public override IEnumerable<NamespaceDefinition> VisibleNamespacesInScope()
	{
		yield return definition;

		foreach (var nsRef in declaration.importedNamespaces)
		{
			var ns = nsRef.Value.definition as NamespaceDefinition;
			if (ns != null)
				yield return ns;
		}

		if (parentScope != null)
			foreach (var ns in parentScope.VisibleNamespacesInScope())
				yield return ns;
	}

	//public override SymbolDefinition AddDeclaration(SymbolKind symbolKind, ParseTree.Node definitionNode)
	//{
	//    SymbolDefinition result;

	//    if (symbolKind != SymbolKind.Namespace)
	//    {
	//        result = base.AddDeclaration(symbolKind, definitionNode);
	//    }
	//    else
	//    {
	//        var symbol = new NamespaceDeclaration { kind = symbolKind, parseTreeNode = definitionNode };
	//        result = AddDeclaration(symbol);
	//    }

	//    result.parentSymbol = definition;
	//    return result;
	//}

	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		if (symbol.kind == SymbolKind.Class ||
		    symbol.kind == SymbolKind.Struct ||
		    symbol.kind == SymbolKind.Interface ||
		    symbol.kind == SymbolKind.Enum ||
		    symbol.kind == SymbolKind.Delegate)
		{
			if (typeDeclarations == null)
				typeDeclarations = new List<SymbolDeclaration>();
			typeDeclarations.Add(symbol);
			symbol.modifiers = (symbol.modifiers & Modifiers.Public) != 0 ? Modifiers.Public : Modifiers.Internal;
		}

		symbol.scope = this;
		return definition.AddDeclaration(symbol);
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (typeDeclarations != null)
			typeDeclarations.Remove(symbol);

		if (definition != null)
			definition.RemoveDeclaration(symbol);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		leaf.resolvedSymbol = null;
		
		SymbolReference symbol;
		if (declaration.typeAliases.TryGetValue(leaf.token.text, out symbol))
		{
			leaf.resolvedSymbol = symbol.definition;
		}
		else
		{
			var parentScopeDef = parentScope != null ? ((NamespaceScope) parentScope).definition : null;
			for (var nsDef = definition;
				leaf.resolvedSymbol == null && nsDef != null && nsDef != parentScopeDef;
				nsDef = nsDef.parentSymbol as NamespaceDefinition)
			{
				nsDef.ResolveMember(leaf, this, numTypeArgs);
			}
			
			if (leaf.resolvedSymbol == null)
			{
				foreach (var nsRef in declaration.importedNamespaces)
				{
					if (nsRef.Value.IsBefore(leaf) && nsRef.Value.definition != null)
					{
						nsRef.Value.definition.ResolveMember(leaf, this, numTypeArgs);
						if (leaf.resolvedSymbol != null)
						{
							if (leaf.resolvedSymbol.kind == SymbolKind.Namespace)
								leaf.resolvedSymbol = null;
							else
								break;
						}
					}
				}
			}
		}

		if (leaf.resolvedSymbol == null && parentScope != null)
			parentScope.Resolve(leaf, numTypeArgs);
	}

	public override void ResolveAttribute(ParseTree.Leaf leaf)
	{
		leaf.resolvedSymbol = null;
		
		SymbolReference symbol;
		if (declaration.typeAliases.TryGetValue(leaf.token.text, out symbol))
		{
			leaf.resolvedSymbol = symbol.definition;
		}
		else
		{
			var parentScopeDef = parentScope != null ? ((NamespaceScope) parentScope).definition : null;
			for (var nsDef = definition;
				leaf.resolvedSymbol == null && nsDef != null && nsDef != parentScopeDef;
				nsDef = nsDef.parentSymbol as NamespaceDefinition)
			{
				nsDef.ResolveAttributeMember(leaf, this);
			}
			
			if (leaf.resolvedSymbol == null)
			{
				foreach (var nsRef in declaration.importedNamespaces)
				{
					if (nsRef.Value.IsBefore(leaf) && nsRef.Value.definition != null)
					{
						nsRef.Value.definition.ResolveAttributeMember(leaf, this);
						if (leaf.resolvedSymbol != null)
							break;
					}
				}
			}
		}

		if (leaf.resolvedSymbol == null && parentScope != null)
			parentScope.ResolveAttribute(leaf);
	}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		return definition.FindName(symbolName, numTypeParameters, true);
	}

	//public virtual SymbolDefinition ImportReflectedType(Type type)
	//{
	//    return definition.ImportReflectedType(type);
	//}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		assembly = GetAssembly();

		//definition.GetCompletionData(data, assembly);
		GetMembersCompletionData(data, BindingFlags.NonPublic, true, assembly);
		foreach (var ta in declaration.typeAliases)
			if (!data.ContainsKey(ta.Key))
				data.Add(ta.Key, ta.Value.definition);
	//	data.UnionWith(declaration.typeAliases.Keys);
		foreach (var i in declaration.importedNamespaces)
			i.Value.definition.GetCompletionData(data, fromInstance, assembly);
		
		var parentScopeDef = parentScope != null ? ((NamespaceScope) parentScope).definition : null;
		for (var nsDef = definition.parentSymbol;
			nsDef != null && nsDef != parentScopeDef;
			nsDef = nsDef.parentSymbol as NamespaceDefinition)
		{
			nsDef.GetCompletionData(data, fromInstance, assembly);
		}
		
		base.GetCompletionData(data, false, assembly);
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly)
	{
		definition.GetMembersCompletionData(data, flags, includePrivate ? AccessLevelMask.Any : AccessLevelMask.Public, assembly);
	}

	public override void GetExtensionMethodsCompletionData(TypeDefinitionBase forType, Dictionary<string, SymbolDefinition> data)
	{
//	Debug.Log("Extensions for " + forType.GetTooltipText());
 		if (parentScope != null)
			GetExtensionMethodsCompletionData(forType, data);
	}
}

public class AttributesScope : Scope
{
	public AttributesScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		Debug.LogException(new InvalidOperationException());
		return null;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		Debug.LogException(new InvalidOperationException());
	}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		var result = parentScope.FindName(symbolName, numTypeParameters);
		return result;
	}

	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly)
	{
		throw new NotImplementedException();
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		leaf.resolvedSymbol = null;
		base.Resolve(leaf, numTypeArgs);

		if (leaf.resolvedSymbol == null || leaf.resolvedSymbol == SymbolDefinition.unknownSymbol)
		{
			if (leaf.parent.RuleName == "typeOrGeneric" && leaf.parent.parent.parent.parent.RuleName == "attribute" &&
				leaf.parent.childIndex == leaf.parent.parent.numValidNodes - 1)
			{
				var old = leaf.token.text;
				leaf.token.text += "Attribute";
				leaf.resolvedSymbol = null;
				base.Resolve(leaf, numTypeArgs);
				leaf.token.text = old;
			}
		}

		//if (leaf.resolvedSymbol == SymbolDefinition.unknownSymbol)
		//	Debug.LogError(leaf);
	}
}

public class LocalScope : Scope
{
	protected List<SymbolDefinition> localSymbols;

	public LocalScope(ParseTree.Node node) : base(node) {}
	
	public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
		symbol.scope = this;
		if (localSymbols == null)
			localSymbols = new List<SymbolDefinition>();

		//var name = symbol.Name;

	//	Debug.Log("Adding localSymbol " + name);
		var definition = SymbolDefinition.Create(symbol);
	//	var oldDefinition = (from ls in localSymbols where ls.Value.declarations[0].parseTreeNode.parent == symbol.parseTreeNode.parent select ls.Key).FirstOrDefault();
	//	if (oldDefinition != null)
	//		Debug.LogWarning(oldDefinition);
		localSymbols.Add(definition);

		return definition;
	}

	public override void RemoveDeclaration(SymbolDeclaration symbol)
	{
		if (localSymbols != null)
		{
	//		Debug.Log("Removing localSymbol " + symbol.Name);
			localSymbols.RemoveAll((SymbolDefinition x) => {
				if (x.declarations == null)
					return false;
				if (!x.declarations.Remove(symbol))
					return false;
				return x.declarations.Count == 0;
			});
		}
		symbol.definition = null;
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		leaf.resolvedSymbol = null;

		if (localSymbols != null)
		{
			var id = leaf.token.text;
			SymbolDefinition local = localSymbols.Find(x => x.name == id);
			if (local != null)
			{
				leaf.resolvedSymbol = local;
				return;
			}
		}

		base.Resolve(leaf, numTypeArgs);
	}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		symbolName = SymbolDefinition.DecodeId(symbolName);
		
		SymbolDefinition definition = null;
		if (numTypeParameters == 0 && localSymbols != null)
			definition = localSymbols.Find(x => x.name == symbolName);
		return definition;
	}

	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		if (localSymbols != null)
			foreach (var ls in localSymbols)
			{
				SymbolDeclaration declaration = ls.declarations.FirstOrDefault();
				ParseTree.Node declarationNode = declaration != null ? declaration.parseTreeNode : null;
				if (declarationNode == null)
					continue;
				var firstLeaf = declarationNode.GetFirstLeaf();
				if (firstLeaf != null &&
					(firstLeaf.line > completionAtLine ||
					firstLeaf.line == completionAtLine && firstLeaf.tokenIndex >= completionAtTokenIndex))
						continue;
				if (!data.ContainsKey(ls.name))
					data.Add(ls.name, ls);
			}
		base.GetCompletionData(data, fromInstance, assembly);
	}
	
	public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, bool includePrivate, AssemblyDefinition assembly)
	{
		throw new InvalidOperationException();
	}
}

public class AccessorBodyScope : BodyScope
{
	private ValueParameter _value;
	private ValueParameter Value {
		get {
			if (_value == null || !_value.IsValid())
			{
				/*var valueType =*/ definition.parentSymbol.TypeOf();
				_value = new ValueParameter
				{
					name = "value",
					kind = SymbolKind.Parameter,
					parentSymbol = definition,
					type = ((InstanceDefinition) definition.parentSymbol).type,
				};
			}
			return _value;
		}
	}
	
	public AccessorBodyScope(ParseTree.Node node) : base(node) {}

	public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
	{
		symbolName = SymbolDefinition.DecodeId(symbolName);
		
		if (numTypeParameters == 0 && symbolName == "value" && definition.name != "get")
		{
			return Value;
		}

		return base.FindName(symbolName, numTypeParameters);
	}

	public override void Resolve(ParseTree.Leaf leaf, int numTypeArgs)
	{
		if (numTypeArgs == 0 && leaf.token.text == "value" && definition.name != "get")
		{
			leaf.resolvedSymbol = Value;
			return;
		}

		base.Resolve(leaf, numTypeArgs);
	}
		
	public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		if (definition.name != "get")
			data["value"] = Value;
		base.GetCompletionData(data, fromInstance, assembly);
	}
}

public class SymbolDefinition
{
	public static readonly SymbolDefinition nullLiteral = new SymbolDefinition { kind = SymbolKind.Null };
	public static readonly SymbolDefinition contextualKeyword = new SymbolDefinition { kind = SymbolKind.Null };
	public static readonly TypeDefinition unknownType = new TypeDefinition { name = "unknown type", kind = SymbolKind.Error };
	public static readonly SymbolDefinition unknownSymbol = new SymbolDefinition { name = "unknown symbol", kind = SymbolKind.Error };
	public static readonly SymbolDefinition thisInStaticMember = new SymbolDefinition { name = "cannot use 'this' in static member", kind = SymbolKind.Error };
	public static readonly SymbolDefinition baseInStaticMember = new SymbolDefinition { name = "cannot use 'base' in static member", kind = SymbolKind.Error };

	protected static readonly List<ParameterDefinition> _emptyParameterList = new List<ParameterDefinition>();
	protected static readonly List<SymbolReference> _emptyInterfaceList = new List<SymbolReference>();
	
	public SymbolKind kind;
	public string name;

	public SymbolDefinition parentSymbol;

	public Modifiers modifiers;
	public AccessLevel accessLevel;

	/// <summary>
	/// Zero, one, or more declarations defining this symbol
	/// </summary>
	/// <remarks>Check for null!!!</remarks>
	public List<SymbolDeclaration> declarations;

	public Dictionary<string, SymbolDefinition> members = new Dictionary<string, SymbolDefinition>();

	public static AccessLevel AccessLevelFromModifiers(Modifiers modifiers)
	{
		if ((modifiers & Modifiers.Public) != 0)
			return AccessLevel.Public;
		if ((modifiers & Modifiers.Protected) != 0)
		{
			if ((modifiers & Modifiers.Internal) != 0)
				return AccessLevel.ProtectedOrInternal;
			return AccessLevel.Protected;
		}
		if ((modifiers & Modifiers.Internal) != 0)
			return AccessLevel.Internal;
		if ((modifiers & Modifiers.Private) != 0)
			return AccessLevel.Private;
		return AccessLevel.None;
	}
	
	public static string DecodeId(string name)
	{
		if (!string.IsNullOrEmpty(name) && name[0] == '@')
			return name.Substring(1);
		return name;
	}

	public bool IsValid()
	{
		if (declarations == null)
		{
			if (this is ReflectedType && parentSymbol == null)
				return false;
			return true; // kind != SymbolKind.Error;
		}

		for (var i = declarations.Count; i --> 0; )
		{
			if (!declarations[i].IsValid())
			{
				var removing = declarations[i];
				declarations.RemoveAt(i);
				if (removing.scope != null)
					removing.scope.RemoveDeclaration(removing);
			}
		}

		return declarations.Count > 0;
	}

	public static SymbolDefinition Create(SymbolDeclaration declaration)
	{
		var definition = Create(declaration.kind, declaration.Name);
		declaration.definition = definition;

		if (declaration.parseTreeNode != null)
		{
			definition.modifiers = declaration.modifiers;
			definition.accessLevel = AccessLevelFromModifiers(declaration.modifiers);

			if (definition.declarations == null)
				definition.declarations = new List<SymbolDeclaration>();
			definition.declarations.Add(declaration);
		}

		var nameNode = declaration.NameNode();
		if (nameNode is ParseTree.Leaf)
			nameNode.SetDeclaredSymbol(definition);

		return definition;
	}

	public static SymbolDefinition Create(SymbolKind kind, string name)
	{
		SymbolDefinition definition;

		switch (kind)
		{
			case SymbolKind.LambdaExpression:
				definition = new SymbolDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Parameter:
				definition = new ParameterDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.ForEachVariable:
			case SymbolKind.FromClauseVariable:
			case SymbolKind.Variable:
			case SymbolKind.Field:
			case SymbolKind.ConstantField:
			case SymbolKind.LocalConstant:
			case SymbolKind.Property:
			case SymbolKind.Event:
			case SymbolKind.CatchParameter:
			case SymbolKind.EnumMember:
				definition = new InstanceDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Indexer:
				definition = new IndexerDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Struct:
			case SymbolKind.Class:
			case SymbolKind.Enum:
			case SymbolKind.Interface:
				definition = new TypeDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Delegate:
				definition = new DelegateTypeDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Namespace:
				definition = new NamespaceDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Method:
				definition = new MethodDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Constructor:
				definition = new MethodDefinition
				{
				    name = ".ctor",
				};
				break;

			case SymbolKind.MethodGroup:
				definition = new MethodGroupDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.TypeParameter:
				definition = new TypeParameterDefinition
				{
					name = name,
				};
				break;

			case SymbolKind.Accessor:
				definition = new SymbolDefinition
				{
					name = name,
				};
				break;

			default:
				definition = new SymbolDefinition
				{
					name = name,
				};
				break;
		}

		definition.kind = kind;

		return definition;
	}

	public virtual string GetName()
	{
		var typeParameters = GetTypeParameters();
		if (typeParameters == null || typeParameters.Count == 0)
			return name;

		var sb = new StringBuilder();
		sb.Append(name);
		sb.Append('<');
		sb.Append(typeParameters[0].GetName());
		for (var i = 1; i < typeParameters.Count; ++i)
		{
			sb.Append(", ");
			sb.Append(typeParameters[i].GetName());
		}
		sb.Append('>');
		return sb.ToString();
	}

	public string ReflectionName
	{
		get {
			var tp = GetTypeParameters();
			return tp != null && tp.Count > 0 ? name + "`" + tp.Count : name;
		}
	}

	public virtual SymbolDefinition TypeOf()
	{
		return this;
	}
	
	public virtual SymbolDefinition GetGenericSymbol()
	{
		return this;
	}

	public static Dictionary<Type, ReflectedType> reflectedTypes = new Dictionary<Type, ReflectedType>();

	public TypeDefinitionBase ImportReflectedType(Type type)
	{
		ReflectedType reflectedType;
		if (reflectedTypes.TryGetValue(type, out reflectedType))
			return reflectedType;

		if (type.IsArray)
		{
			var elementType = ImportReflectedType(type.GetElementType());
			var arrayType = elementType.MakeArrayType(type.GetArrayRank());
			return arrayType;
		}

		if ((type.IsGenericType || type.ContainsGenericParameters) && !type.IsGenericTypeDefinition)
		{
			var arguments = type.GetGenericArguments();
			var numGenericArgs = arguments.Length;
			var declaringType = type.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				var parentArgs = declaringType.GetGenericArguments();
				numGenericArgs -= parentArgs.Length;
			}

			var argumentRefs = new List<ReflectedTypeReference>(numGenericArgs);
			for (var i = arguments.Length - numGenericArgs; i < arguments.Length; ++i)
				argumentRefs.Add(ReflectedTypeReference.ForType(arguments[i]));

			var typeDefinitionRef = ReflectedTypeReference.ForType(type.GetGenericTypeDefinition());
			var typeDefinition = typeDefinitionRef.definition as TypeDefinition;
			var constructedType = typeDefinition.ConstructType(argumentRefs.ToArray());
			return constructedType;
		}

		if (type.IsGenericParameter)
		{
			UnityEngine.Debug.LogError("Importing reflected generic type parameter " + type.FullName);
			//var arguments = type.GetGenericArguments();
			//var argumentRefs = new ReflectedTypeReference[arguments.Length];
			//for (var i = 0; i < arguments.Length; ++i)
			//    argumentRefs[i] = ReflectedTypeReference.ForType(arguments[i]);

			//var typeDefinition = ImportReflectedType(type.GetGenericTypeDefinition()) as TypeDefinition;
			//var constructedType = new ConstructedTypeDefinition(typeDefinition, argumentRefs);
			//return constructedType;
		}

		reflectedTypes[type] = reflectedType = new ReflectedType(type);
		members[reflectedType.ReflectionName] = reflectedType;
		reflectedType.parentSymbol = this;
		//if (type.IsSubclassOf(typeof(Attribute)) && reflectedType.name.EndsWith("Attribute") && reflectedType.name != "Attribute")
		//    members[reflectedType.name.Substring(0, reflectedType.name.Length - "Attribute".Length)] = reflectedType;
		return reflectedType;
	}

	public SymbolDefinition ImportReflectedMethod(MethodInfo info)
	{
		var imported = new ReflectedMethod(info, this);
		SymbolDefinition methodGroup;
		if (!members.TryGetValue(imported.ReflectionName, out methodGroup))
		{
			methodGroup = Create(SymbolKind.MethodGroup, imported.GetName());
			members[imported.ReflectionName] = methodGroup;
			methodGroup.parentSymbol = this;
		}
		//else
		//{
		//    UnityEngine.Debug.Log("Importing reflected method overload " + imported);
		//}
		((MethodGroupDefinition)methodGroup).AddMethod(imported);
	//	Debug.Log("Importing reflected method " + imported.GetTooltipText());
		return methodGroup;
	}

	public SymbolDefinition ImportReflectedConstructor(ConstructorInfo info)
	{
		var imported = new ReflectedConstructor(info, this);
		members[".ctor"] = imported;
		return imported;
	}

	//public virtual SymbolDefinition AddMember(SymbolKind kind, string name)
	//{
	//    var member = CreateSymbolDefinition(kind, name, null);
	//    member.parentSymbol = this;
	//    if (!string.IsNullOrEmpty(name))
	//        members[name] = member;
	//    return member;
	//}

	public void AddMember(SymbolDefinition symbol)
	{
		symbol.parentSymbol = this;
		if (!string.IsNullOrEmpty(symbol.name))
		{
			var declaration = symbol.declarations != null && symbol.declarations.Count == 1 ? symbol.declarations[0] : null;
			if (declaration != null && declaration.numTypeParameters > 0)
				members[declaration.ReflectionName] = symbol;
			else
				members[symbol.ReflectionName] = symbol;
		}
	}

	public virtual SymbolDefinition AddMember(SymbolDeclaration symbol)
	{
		var symbolName = symbol.Name;
		var member = Create(symbol);
		if (member.kind == SymbolKind.Method)
		{
			SymbolDefinition methodGroup = null;
			if (!members.TryGetValue(member.ReflectionName, out methodGroup))
			{
				methodGroup = AddMember(new SymbolDeclaration(symbolName)
				{
					kind = SymbolKind.MethodGroup,
					modifiers = symbol.modifiers,
					parseTreeNode = symbol.parseTreeNode,
					scope = symbol.scope,
				});
			//	methodGroup.parentSymbol = this;
			}
			if (methodGroup is MethodGroupDefinition)
			{
				((MethodGroupDefinition) methodGroup).AddMethod((MethodDefinition) member);
			//	member = methodGroup;
			}
			//else
			//	UnityEngine.Debug.LogError(methodGroup);
		}
		else
		{
			if (member.kind == SymbolKind.Delegate)
			{
				var memberAsDelegate = (DelegateTypeDefinition) member;
				memberAsDelegate.returnType = new SymbolReference(symbol.parseTreeNode.ChildAt(1));
				//var paramsNode = symbol.parseTreeNode.FindChildByName("formalParameterList") as ParseTree.Node;
				//if (paramsNode != null && paramsNode.nodes != null)
				//{
				//    for (var i = 0; i < paramsNode.nodes.Count; i += 2)
				//    {
				//        var parameterNode = paramsNode.nodes[i] as ParseTree.Node;
				//        if (parameterNode != null)
				//        {
				//            memberAsDelegate.AddParameter(
				//                new SymbolDeclaration
				//                {
				//                    kind = SymbolKind.Parameter,
				//                    parseTreeNode = parameterNode,
				//                    scope = paramsNode.scope
				//                });
				//        }
				//    }
				//}
			}

			AddMember(member);
		}

		return member;
	}

	public virtual SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
	{
//		if (symbol.parseTreeNode != null)
//			declarations.Add(symbol);

		var parentNamespace = this as NamespaceDefinition;

		var symbolName = symbol.ReflectionName;
		SymbolDefinition definition;
		if (parentNamespace != null && symbol is NamespaceDeclaration)
		{
			var qnNode = symbol.parseTreeNode.NodeAt(1);
			if (qnNode == null)
				return null;

			for (var i = 0; i < qnNode.numValidNodes - 2; i += 2)
			{
				var ns = qnNode.ChildAt(i).Print();
				var childNS = FindName(ns, 0, false);
				if (childNS == null)
				{
					childNS = new NamespaceDefinition {
						kind = SymbolKind.Namespace,
						name = ns,
						accessLevel = AccessLevel.Public,
						modifiers = Modifiers.Public,
					};
					parentNamespace.AddMember(childNS);
				}
				parentNamespace = childNS as NamespaceDefinition;
				if (parentNamespace == null)
					break;
			}
		}

		if (!members.TryGetValue(symbolName, out definition) ||
			symbol.kind == SymbolKind.Method && definition is MethodGroupDefinition ||
			!definition.IsValid() ||
			definition is ReflectedMember || definition is ReflectedType) // TODO: Check this!!!
		{
			if (definition != null && definition is ReflectedType && definition != symbol.definition)
				definition.parentSymbol = null;
			definition = (parentNamespace ?? this).AddMember(symbol);
		}
		else
		{
			// TODO: Hack? Rework this block eventually!
			if (definition.declarations != null)
			{
				for (var i = definition.declarations.Count; i --> 0; )
				{
					var decl = definition.declarations[i];
					if (!decl.IsValid() || definition.IsValid())
					{
						definition = AddMember(symbol);
						break;
					}
				}
			}
		}

		symbol.definition = definition;

		var nameNode = symbol.NameNode();
		if (nameNode != null)
		{
			var leaf = nameNode as ParseTree.Leaf;
			if (leaf != null)
			{
				leaf.SetDeclaredSymbol(definition);
			}
			else
			{
				var lastLeaf = ((ParseTree.Node) nameNode).GetLastLeaf();
				if (lastLeaf != null)
				{
					if (lastLeaf.parent.RuleName == "typeParameterList")
						lastLeaf = lastLeaf.parent.parent.LeafAt(0);
					lastLeaf.SetDeclaredSymbol(definition);
				}
			}
		}

		return definition;
	}

	public virtual void RemoveDeclaration(SymbolDeclaration symbol)
	{
		SymbolDefinition m;
		if (!members.TryGetValue(symbol.ReflectionName, out m))
		{
			//Debug.Log("Can't remove declaration " + symbol.ReflectionName);
			return;
		}
		if (m.kind == SymbolKind.MethodGroup)
		{
			var mg = m as MethodGroupDefinition;
			//Debug.Log("Removing declaration in " + mg.GetTooltipText());
			mg.RemoveDeclaration(symbol);
			if (mg.methods.Count == 0)
			{
				mg.declarations.Clear();
				members.Remove(mg.ReflectionName);
				mg.parentSymbol = null;
			}
		}
		else
		{
			if (m.declarations != null && m.declarations.Remove(symbol))
			{
				if (m.declarations.Count == 0)
					members.Remove(symbol.ReflectionName);
			}
		}
	}

	public override string ToString()
	{
		return kind + " " + name;
	}

	public virtual string GetDelegateInfoText() { return GetTooltipText(); }

	public string PrintParameters(List<ParameterDefinition> parameters, bool singleLine = false)
	{
		if (parameters == null)
			return "";

		var parametersText = "";
		var comma = !singleLine && parameters.Count > 1 ? "\n\t" : string.Empty;
		var nextComma = !singleLine && parameters.Count > 1 ? ",\n\t" : ", ";
		foreach (var param in parameters)
		{
			if (param == null)
				continue;
			var typeOfP = param.TypeOf() as TypeDefinitionBase;
			if (typeOfP == null)
				continue;

			//var cs = this as ConstructedSymbolDefinition;
			//if (cs != null)
			//{
			var ctx = (kind == SymbolKind.Delegate ? this : parentSymbol) as ConstructedTypeDefinition;
			if (ctx != null)
				typeOfP = typeOfP.SubstituteTypeParameters(ctx);
			//}

//			var tp = typeOfP as TypeParameterDefinition;
//			if (tp != null)
//				typeOfP = TypeOfTypeParameter(tp);
			if (typeOfP == null)
				continue;
			parametersText += comma;
			if (param.IsThisParameter)
				parametersText += "this ";
			else if (param.IsRef)
				parametersText += "ref ";
			else if (param.IsOut)
				parametersText += "out ";
			else if (param.IsParametersArray)
				parametersText += "params ";
			parametersText += typeOfP.GetName() + ' ' + param.name;
			if (param.defaultValue != null)
				parametersText += " = " + param.defaultValue;
			comma = nextComma;
		}
		if (!singleLine && parameters.Count > 1)
			parametersText += '\n';
		return parametersText;
	}

	protected string tooltipText;

	public virtual string GetTooltipText()
	{
		if (kind == SymbolKind.Null)
			return null;

//		if (tooltipText != null)
//			return tooltipText;

		if (kind == SymbolKind.Error)
			return tooltipText = name;

		var kindText = string.Empty;
		switch (kind)
		{
			case SymbolKind.Namespace: return tooltipText = "namespace " + FullName;
			case SymbolKind.Constructor: kindText = "(constructor) "; break;
			case SymbolKind.Destructor: kindText = "(destructor) "; break;
			case SymbolKind.ConstantField:
			case SymbolKind.LocalConstant: kindText = "(constant) "; break;
			case SymbolKind.Property: kindText = "(property) "; break;
			case SymbolKind.Event: kindText = "(event) "; break;
			case SymbolKind.Variable:
			case SymbolKind.ForEachVariable:
			case SymbolKind.FromClauseVariable:
			case SymbolKind.CatchParameter: kindText = "(local variable) "; break;
			case SymbolKind.Parameter: kindText = "(parameter) "; break;
			case SymbolKind.Delegate: kindText = "delegate "; break;
			case SymbolKind.MethodGroup: kindText = "(method group) "; break;
			case SymbolKind.Accessor: kindText = "(accessor) "; break;
			case SymbolKind.Label: return tooltipText = "(label) " + name;
		}

		var typeOf =
			/*kind == SymbolKind.Delegate ? TypeOf()//((DelegateTypeDefinition) this).returnType.definition
			:*/ kind == SymbolKind.Accessor || kind == SymbolKind.MethodGroup ? null
			: TypeOf();
		var typeName = string.Empty;
		if (typeOf != null && kind != SymbolKind.Namespace && kind != SymbolKind.Constructor && kind != SymbolKind.Destructor)
		{
			//var tp = typeOf as TypeParameterDefinition;
			//if (tp != null)
			//    typeOf = TypeOfTypeParameter(tp);
			var ctx = (typeOf.kind == SymbolKind.Delegate ? typeOf : parentSymbol) as ConstructedTypeDefinition;
			if (ctx != null)
				typeOf = ((TypeDefinitionBase) typeOf).SubstituteTypeParameters(ctx);
			typeName = typeOf.GetName() + " ";

			if (typeOf.kind != SymbolKind.TypeParameter)
				for (var parentType = typeOf.parentSymbol as TypeDefinitionBase; parentType != null; parentType = parentType.parentSymbol as TypeDefinitionBase)
					typeName = parentType.GetName() + '.' + typeName;
		}

		var parentText = string.Empty;
		var parent = parentSymbol is MethodGroupDefinition ? parentSymbol.parentSymbol : parentSymbol;
		if ((parent is TypeDefinitionBase && parent.kind != SymbolKind.Delegate && kind != SymbolKind.TypeParameter)
			|| parent is NamespaceDefinition
			)//|| kind == SymbolKind.Accessor)
		{
			var parentName = parent.GetName();
			if (kind == SymbolKind.Constructor)
			{
				var typeParent = parent.parentSymbol as TypeDefinitionBase;
				parentName = typeParent != null ? typeParent.GetName() : null;
			}
			if (!string.IsNullOrEmpty(parentName))
				parentText = parentName + ".";
		}

		var nameText = GetName();

		//List<TypeParameterDefinition> typeParameters = GetTypeParameters();
	    //if (typeParameters != null && typeParameters.Count > 0)
	    //{
		//	nameText += "<" + typeParameters[0].GetName();
	    //    for (var i = 1; i < typeParameters.Count; ++i)
	    //        nameText += ", " + typeParameters[i].GetName();
	    //    nameText += '>';
	    //}

		var parameters = GetParameters();
		var parametersText = string.Empty;
		string parametersEnd = null;
		
		if (kind == SymbolKind.Method)
		{
			nameText += (parameters.Count == 1 ? "( " : "(");
			//parameters = ((MethodDefinition) this).parameters;
			parametersEnd = (parameters.Count == 1 ? " )" : ")");
		}
		else if (kind == SymbolKind.Constructor)
		{
			nameText = parent.name + '(';
			parametersEnd = ")";
		}
		else if (kind == SymbolKind.Destructor)
		{
			nameText = "~" + parent.name + "()";
		}
		else if (kind == SymbolKind.Indexer)
		{
			nameText = (parameters.Count == 1 ? "this[ " : "this[");
			parametersEnd = (parameters.Count == 1 ? " ]" : "]");
		}
		else if (kind == SymbolKind.Delegate)
		{
			nameText += (parameters.Count == 1 ? "( " : "(");
			parametersEnd = (parameters.Count == 1 ? " )" : ")");
		}

		if (parameters != null)
		{
			parametersText = PrintParameters(parameters);
		}

		tooltipText = kindText + typeName + parentText + nameText + parametersText + parametersEnd;

		if (typeOf != null && typeOf.kind == SymbolKind.Delegate)
		{
			tooltipText += "\n\nDelegate info\n";
			tooltipText += typeOf.GetDelegateInfoText();
		}

		var xmlDocs = GetXmlDocs();
		if (!string.IsNullOrEmpty(xmlDocs))
		{
		    tooltipText += "\n\n" + xmlDocs;
		}

		return tooltipText;
	}

	public virtual List<ParameterDefinition> GetParameters()
	{
		return null;
	}

	public virtual List<TypeParameterDefinition> GetTypeParameters()
	{
		return null;
	}

	public string GetXmlDocs()
	{
#if UNITY_WEBPLAYER
		return null;
#else
		string result = null;
		
		var unityName = UnityHelpName;
		if (unityName != null)
		{
			if (UnitySymbols.summaries.TryGetValue(unityName, out result))
				return result;
		//	Debug.Log(unityName);
			return null;
		}
		
		return result;
#endif
		
	    //var xml = new System.Xml.XmlDocument();
	    //xml.Load(UnityEngine.Application.dataPath + "/FlipbookGames/ScriptInspector2/Editor/EditorResources/XmlDocs/UnityEngine.xml");
	    //var summary = xml.SelectSingleNode("/doc/members/member[@name = 'T:" + FullName + "']/summary");
	    //if (summary != null)
	    //    return summary.InnerText;
	    //return null;
	}

	public string UnityHelpName
	{
		get
		{
			if (kind == SymbolKind.TypeParameter)
				return null;
			
			var result = FullName;
			if (result == null)
				return null;
			if (result.StartsWith("UnityEngine."))
				result = result.Substring("UnityEngine.".Length);
			else if (result.StartsWith("UnityEditor."))
				result = result.Substring("UnityEditor.".Length);
			else
				return null;
			
			if (kind == SymbolKind.Indexer)
				result = result.Substring(0, result.LastIndexOf('.') + 1) + "Index_operator";
			else if (kind == SymbolKind.Constructor)
				result = result.Substring(0, result.LastIndexOf('.')) + "-ctor";
			else if ((kind == SymbolKind.Field || kind == SymbolKind.Property) && parentSymbol.kind != SymbolKind.Enum)
				result = result.Substring(0, result.LastIndexOf('.')) + "-" + name;
			
			return result;
		}
	}
	
	protected int IndexOfTypeParameter(TypeParameterDefinition tp)
	{
		var typeParams = GetTypeParameters();
		var index = typeParams != null ? typeParams.IndexOf(tp) : -1;
		if (index < 0)
			return parentSymbol != null ? parentSymbol.IndexOfTypeParameter(tp) : -1;
		for (var parent = parentSymbol; parent != null; parent = parent.parentSymbol)
		{
			typeParams = parent.GetTypeParameters();
			if (typeParams != null)
				index += typeParams.Count;
		}
		return index;
	}
	
	public string XmlDocsName
	{
		get
		{
			var sb = new StringBuilder();
			switch (kind)
			{
				case SymbolKind.Namespace:
					sb.Append("N:");
					sb.Append(FullName);
					break;
				case SymbolKind.Class:
				case SymbolKind.Struct:
				case SymbolKind.Interface:
				case SymbolKind.Enum:
				case SymbolKind.Delegate:
					sb.Append("T:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Field:
				case SymbolKind.ConstantField:
					sb.Append("F:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Property:
					sb.Append("P:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Indexer:
					sb.Append("P:");
					sb.Append(parentSymbol.FullReflectionName);
					sb.Append(".Item");
					break;
				case SymbolKind.Method:
				case SymbolKind.Operator:
					sb.Append("M:");
					sb.Append(FullReflectionName);
					break;
				case SymbolKind.Constructor:
					sb.Append("M:");
					sb.Append(parentSymbol.FullReflectionName);
					sb.Append(".#ctor");
					break;
				case SymbolKind.Destructor:
					sb.Append("M:");
					sb.Append(parentSymbol.FullReflectionName);
					sb.Append(".Finalize");
					break;
				case SymbolKind.Event:
					sb.Append("E:");
					sb.Append(FullReflectionName);
					break;
				default:
					return null;
			}
			var parameters = GetParameters();
			if (parameters != null && parameters.Count > 0)
			{
				sb.Append("(");
				for (var i = 0; i < parameters.Count; ++i)
				{
					var p = parameters[i];
					if (i > 0)
						sb.Append(",");
					var t = p.TypeOf();
					if (t.kind == SymbolKind.TypeParameter)
					{
						sb.Append('`');
						var tp = t as TypeParameterDefinition;
						var tpIndex = tp.parentSymbol.IndexOfTypeParameter(tp);
						sb.Append(tpIndex);
					}
					else
					{
						sb.Append(t.FullReflectionName);
					}
					var a = t as ArrayTypeDefinition;
					if (a != null)
					{
						if (a.rank == 1)
						{
							sb.Append("[]");
						}
						else
						{
							sb.Append("[0:");
							for (var j = 1; j < a.rank; ++j)
								sb.Append(",0:");
							sb.Append("]");
						}
					}
					else if (p.IsRef || p.IsOut)
						sb.Append("@");
					if (p.IsOptional)
						sb.Append("!");
				}
				sb.Append(")");
			}
			return sb.ToString();
		}
	}

	public string FullName
	{
		get
		{
			if (parentSymbol != null)
			{
				var parentFullName = (parentSymbol is MethodGroupDefinition) ? parentSymbol.parentSymbol.FullName : parentSymbol.FullName;
				if (string.IsNullOrEmpty(name))
					return parentFullName;
				if (string.IsNullOrEmpty(parentFullName))
					return name;
				return parentFullName + '.' + name;
			}
			return name;
		}
	}

	public string FullReflectionName
	{
		get
		{
			if (parentSymbol != null)
			{
				var parentFullName = (parentSymbol is MethodGroupDefinition) ? parentSymbol.parentSymbol.FullReflectionName : parentSymbol.FullReflectionName;
				if (string.IsNullOrEmpty(ReflectionName))
					return parentFullName;
				if (string.IsNullOrEmpty(parentFullName))
					return ReflectionName;
				return parentFullName + '.' + ReflectionName;
			}
			return ReflectionName;
		}
	}

	public string Dump()
	{
		var sb = new StringBuilder();
		Dump(sb, string.Empty);
		return sb.ToString();
	}

	protected virtual void Dump(StringBuilder sb, string indent)
	{
		sb.AppendLine(indent + kind + " " + name + " (" + GetType() + ")");

		foreach (var member in members)
			member.Value.Dump(sb, indent + "  ");
	}

	public virtual void ResolveMember(ParseTree.Leaf leaf, Scope context, int numTypeArgs)
	{
		leaf.resolvedSymbol = null;

		var id = DecodeId(leaf.token.text);

		SymbolDefinition definition;
		if (!members.TryGetValue(numTypeArgs > 0 ? id + "`" + numTypeArgs : id, out definition))
		{
			var marker = id.IndexOf('`');
			if (marker > 0)
			{
				Debug.LogError("ResolveMember!!! " + id);
				members.TryGetValue(id.Substring(0, marker), out definition);
			}
		}
		if (definition != null && definition.kind != SymbolKind.Namespace && !(definition is TypeDefinitionBase))
		{
			if (leaf.parent.RuleName == "typeOrGeneric")
				return;
		}

		leaf.resolvedSymbol = definition;
	}

	public virtual void ResolveAttributeMember(ParseTree.Leaf leaf, Scope context)
	{
		leaf.resolvedSymbol = null;

		var id = leaf.token.text;
		leaf.resolvedSymbol = FindName(id, 0, true) ?? FindName(id + "Attribute", 0, true);
	}
	
	public static Dictionary<string, TypeDefinitionBase> builtInTypes;
	//public static HashSet<string> missingResolveNodePaths = new HashSet<string>();
	
	public static SymbolDefinition ResolveNodeAsExtensionMethod(ParseTree.BaseNode node, Scope scope)
	{
		//Debug.Log("Trying to resolve " + node.Print() + " as extension method");
		return null;
	}

	public static SymbolDefinition ResolveNodeAsConstructor(ParseTree.BaseNode oceNode, Scope scope, SymbolDefinition asMemberOf)
	{
		if (asMemberOf == null)
			return null;

		var node = oceNode as ParseTree.Node;
		if (node == null || node.numValidNodes == 0)
			return null;

		var node1 = node.NodeAt(0);
		if (node1 == null)
			return null;

		var constructor = asMemberOf.FindName(".ctor", 0, false);
		if (constructor == null || constructor.parentSymbol != asMemberOf)
			constructor = ((TypeDefinitionBase) asMemberOf).GetDefaultConstructor();
		if (constructor is MethodGroupDefinition)
		{
			if (node1.RuleName == "arguments")
				constructor = ResolveNode(node1, scope, constructor);
		}
		else if (node1.RuleName == "arguments")
		{
			for (var i = 1; i < node1.numValidNodes - 1; ++i)
				ResolveNode(node1.ChildAt(i), scope, constructor);
		}

		if (node.numValidNodes == 2)
			ResolveNode(node.ChildAt(1));
		
		return constructor;
	}

	public static SymbolDefinition EnumerableElementType(ParseTree.Node node)
	{
		var enumerableExpr = ResolveNode(node);
		if (enumerableExpr != null)
		{
			var arrayType = enumerableExpr.TypeOf() as ArrayTypeDefinition;
			if (arrayType != null)
			{
				if (arrayType.rank > 0 && arrayType.elementType != null)
					return arrayType.elementType;
			}
			else
			{
				var enumerableType = enumerableExpr.TypeOf() as TypeDefinitionBase;
				if (enumerableType != null)
				{
					var assemblyDefinition = AssemblyDefinition.FromAssembly(typeof(IEnumerable<>).Assembly);
					var iEnumerableGenericTypeDef = (TypeDefinitionBase) assemblyDefinition.FindNamespace("System.Collections.Generic").FindName("IEnumerable", 1, true);

					if (enumerableType.DerivesFromRef(ref iEnumerableGenericTypeDef))
					{
						var asGenericEnumerable = iEnumerableGenericTypeDef as ConstructedTypeDefinition;
						if (asGenericEnumerable != null)
							return asGenericEnumerable.typeArguments[0].definition;
					}

					var iEnumerableTypeDef = (TypeDefinition) assemblyDefinition.FindNamespace("System.Collections").FindName("IEnumerable", 0, true);
					if (enumerableType.DerivesFrom(iEnumerableTypeDef))
						return builtInTypes["object"];
				}
			}
		}
		return unknownType;
	}

	public static SymbolDefinition ResolveNode(ParseTree.BaseNode baseNode, Scope scope = null, SymbolDefinition asMemberOf = null, int numTypeArguments = 0)
	{
		if (scope == null)
		{
			var scopeNode = CsGrammar.EnclosingSemanticNode(baseNode, SemanticFlags.ScopesMask);
			while (scopeNode != null && scopeNode.scope == null && scopeNode.parent != null)
				scopeNode = CsGrammar.EnclosingSemanticNode(scopeNode.parent, SemanticFlags.ScopesMask);
			if (scopeNode != null)
				scope = scopeNode.scope;
		}

		var leaf = baseNode as ParseTree.Leaf;
		if (leaf != null)
		{
			if ((leaf.resolvedSymbol == null || leaf.semanticError != null ||
				!leaf.resolvedSymbol.IsValid()) && leaf.token != null)
			{
				leaf.resolvedSymbol = null;

				switch (leaf.token.tokenKind)
				{
					case SyntaxToken.Kind.Identifier:
						if (asMemberOf != null)
						{
							asMemberOf.ResolveMember(leaf, scope, numTypeArguments);
							//if (leaf.resolvedSymbol == null)
							//	UnityEngine.Debug.LogWarning("Could not resolve member '" + leaf + "' of " + asMemberOf + "[" + asMemberOf.GetType() + "], line " + (1+leaf.line));
						}
						else if (scope != null)
						{
							if (leaf.token.text == "global")
							{
								var nextLeaf = leaf.FindNextLeaf();
								if (nextLeaf != null && nextLeaf.IsLit("::"))
								{
									var assembly = scope.GetAssembly();
									if (assembly != null)
									{
										leaf.resolvedSymbol = scope.GetAssembly().GlobalNamespace;
										nextLeaf = nextLeaf.FindNextLeaf();
										if (nextLeaf != null && nextLeaf.token.tokenKind == SyntaxToken.Kind.Identifier)
										{
											nextLeaf.resolvedSymbol = assembly.FindNamespace(nextLeaf.token.text);
										}
										return leaf.resolvedSymbol;
									}
								}
							}
							scope.Resolve(leaf, numTypeArguments);
							//if (leaf.resolvedSymbol == null)
							//	UnityEngine.Debug.LogWarning("Could not resolve '" + leaf + "' in " + scope + ", line " + (1+leaf.line));
						//	if (leaf.token.text == "Test")
						//		Debug.Log("Resolved leaf Test as " + leaf.resolvedSymbol);
						}
						break;

					case SyntaxToken.Kind.Keyword:
						if (leaf.token.text == "this" || leaf.token.text == "base")
						{
							var scopeNode = CsGrammar.EnclosingScopeNode(leaf.parent,
								SemanticFlags.MethodBodyScope,
								SemanticFlags.AccessorBodyScope);//,
								//SemanticFlags.LambdaExpressionBodyScope,
								//SemanticFlags.AnonymousMethodBodyScope);
							if (scopeNode == null)
							{
								leaf.resolvedSymbol = unknownSymbol;
								break;
							}

							var memberScope = scopeNode.scope as BodyScope;
							if (memberScope != null && memberScope.definition.IsStatic)
							{
								if (leaf.token.text == "base")
									leaf.resolvedSymbol = baseInStaticMember;
								else
									leaf.resolvedSymbol = thisInStaticMember;
								break;
							}

							scopeNode = CsGrammar.EnclosingScopeNode(scopeNode, SemanticFlags.TypeDeclarationScope);
							if (scopeNode == null)
							{
								leaf.resolvedSymbol = unknownSymbol;
								break;
							}

							var thisType = ((SymbolDeclarationScope) scopeNode.scope).declaration.definition as TypeDefinitionBase;
							if (thisType != null && leaf.token.text == "base")
								thisType = thisType.BaseType();
							if (thisType != null && (thisType.kind == SymbolKind.Struct || thisType.kind == SymbolKind.Class))
								leaf.resolvedSymbol = thisType.GetThisInstance();
							else
								leaf.resolvedSymbol = unknownSymbol;
							break;
						}
						else
						{
							TypeDefinitionBase type;
							if (builtInTypes.TryGetValue(leaf.token.text, out type))
								leaf.resolvedSymbol = type;
						}
						break;

					case SyntaxToken.Kind.CharLiteral:
						leaf.resolvedSymbol = builtInTypes["char"].GetThisInstance();
						break;

					case SyntaxToken.Kind.IntegerLiteral:
						var endsWith = leaf.token.text[leaf.token.text.Length - 1];
						var unsignedDecimal = endsWith == 'u' || endsWith == 'U';
						var longDecimal = endsWith == 'l' || endsWith == 'L';
						if (unsignedDecimal)
						{
							endsWith = leaf.token.text[leaf.token.text.Length - 2];
							longDecimal = endsWith == 'l' || endsWith == 'L';
						}
						else if (longDecimal)
						{
							endsWith = leaf.token.text[leaf.token.text.Length - 2];
							unsignedDecimal = endsWith == 'u' || endsWith == 'U';
						}
						leaf.resolvedSymbol =
							longDecimal ? (unsignedDecimal ? builtInTypes["ulong"].GetThisInstance() : builtInTypes["long"].GetThisInstance())
							: unsignedDecimal ? builtInTypes["uint"].GetThisInstance() : builtInTypes["int"].GetThisInstance();
						break;

					case SyntaxToken.Kind.RealLiteral:
						endsWith = leaf.token.text[leaf.token.text.Length - 1];
						leaf.resolvedSymbol =
							endsWith == 'f' || endsWith == 'F' ? builtInTypes["float"].GetThisInstance() :
							endsWith == 'm' || endsWith == 'M' ? builtInTypes["decimal"].GetThisInstance() :
							builtInTypes["double"].GetThisInstance();
						break;

					case SyntaxToken.Kind.StringLiteral:
					case SyntaxToken.Kind.VerbatimStringBegin:
					case SyntaxToken.Kind.VerbatimStringLiteral:
						leaf.resolvedSymbol = builtInTypes["string"].GetThisInstance();
						break;

					case SyntaxToken.Kind.BuiltInLiteral:
						leaf.resolvedSymbol = leaf.token.text == "null" ? nullLiteral : builtInTypes["bool"].GetThisInstance();
						break;
				}

				if (leaf.resolvedSymbol == null)
					leaf.resolvedSymbol = unknownSymbol;
			}
			return leaf.resolvedSymbol;
		}

		var node = (ParseTree.Node) baseNode;
		if (node == null || node.numValidNodes == 0 || node.missing)
			return unknownSymbol;

		int rank;
		SymbolDefinition part = null, dummy = null; // used as non-null return value for explicitly resolving child nodes

//		Debug.Log("Resolving node: " + node);
		switch (node.RuleName)
		{
			case "localVariableType":
				if (node.numValidNodes == 1)
					return ResolveNode(node.ChildAt(0), scope, asMemberOf);
				break;

			case "GET":
			case "SET":
			case "ADD":
			case "REMOVE":
				SymbolDeclaration declaration = null;
				for (var tempNode = node; declaration == null && tempNode != null; tempNode = tempNode.parent)
					declaration = tempNode.declaration;
				if (declaration == null)
					return node.ChildAt(0).resolvedSymbol = unknownSymbol;
				return node.ChildAt(0).resolvedSymbol = declaration.definition;

			case "YIELD":
			case "FROM":
			case "SELECT":
			case "WHERE":
			case "GROUP":
			case "INTO":
			case "ORDERBY":
			case "JOIN":
			case "LET":
			case "ON":
			case "EQUALS":
			case "BY":
			case "ASCENDING":
			case "DESCENDING":
				node.ChildAt(0).resolvedSymbol = contextualKeyword;
				return contextualKeyword;

			case "memberName":
				declaration = null;
				while (declaration == null && node != null)
				{
					declaration = node.declaration;
					node = node.parent;
				}
				if (declaration == null)
					return unknownSymbol;
				return declaration.definition;

			case "VAR":
				ParseTree.Node varDeclsNode = null;
				if (node.parent.parent.RuleName == "foreachStatement" && node.parent.parent.numValidNodes >= 6)
				{
					varDeclsNode = node.parent.parent.NodeAt(5);
					if (varDeclsNode != null && varDeclsNode.numValidNodes == 1)
					{
						node.ChildAt(0).resolvedSymbol = EnumerableElementType(varDeclsNode);
					}
				}
				else if (node.parent.parent.numValidNodes >= 2)
				{
					varDeclsNode = node.parent.parent.NodeAt(1);
					if (varDeclsNode != null && varDeclsNode.numValidNodes == 1)
					{
						var declNode = varDeclsNode.NodeAt(0);
						if (declNode != null && declNode.numValidNodes == 3)
						{
							var initExpr = ResolveNode(declNode.ChildAt(2));
							if (initExpr != null)
								node.ChildAt(0).resolvedSymbol = initExpr.TypeOf();
							else
								node.ChildAt(0).resolvedSymbol = unknownType;
						}
						else
							node.ChildAt(0).resolvedSymbol = unknownType;
					}
				}
				else
					node.ChildAt(0).resolvedSymbol = unknownType;
				return node.ChildAt(0).resolvedSymbol;

			case "type": case "type2":
				var typeNodeType = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArguments) as TypeDefinitionBase;
				if (typeNodeType != null)
				{
					if (node.numValidNodes > 1)
					{
						// TODO: check nullable
						var rankNode = node.NodeAt(-1);
						if (rankNode != null)
						{
							typeNodeType = typeNodeType.MakeArrayType(rankNode.numValidNodes - 1);
						}
					}
					return typeNodeType;
				}
				break;

			case "attribute":
				var attributeTypeName = ResolveNode(node.ChildAt(0), scope);
				//if (attributeTypeName == null || attributeTypeName == unknownSymbol || attributeTypeName == unknownType)
				//{
				//    var lastLeaf = ((ParseTree.Node) node.nodes[0]).GetLastLeaf();
				//    var oldText = lastLeaf.token.text;
				//    lastLeaf.token.text += "Attribute";
				//    lastLeaf.resolvedSymbol = null;
				//    attributeTypeName = ResolveNode(node.nodes[0], scope);
				//    lastLeaf.token.text = oldText;
				//}
				if (node.numValidNodes == 2)
					ResolveNode(node.ChildAt(1), null);
				return attributeTypeName;

			case "integralType":
			case "simpleType":
			case "numericType":
			case "floatingPointType":
			case "predefinedType":
			case "typeName":
			case "exceptionClassType":
				return ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArguments) as TypeDefinitionBase;

			case "nonArrayType":
				var nonArrayTypeSymbol = ResolveNode(node.ChildAt(0), scope, asMemberOf);
				var nonArrayType = nonArrayTypeSymbol as TypeDefinitionBase;
				if (nonArrayType != null && node.numValidNodes == 2)
					return nonArrayType.MakeNullableType();
				return nonArrayType;

			//case "typeParameterList":
			//    return null;

			case "typeParameter":
				return ResolveNode(node.ChildAt(0), scope, asMemberOf) as TypeDefinitionBase;

			case "typeVariableName":
				asMemberOf = ((SymbolDeclarationScope) scope).declaration.definition;
				return ResolveNode(node.ChildAt(0), scope, asMemberOf) as TypeParameterDefinition;

			case "typeOrGeneric":
				if (asMemberOf == null && node.childIndex > 0)
					asMemberOf = ResolveNode(node.parent.ChildAt(node.childIndex - 2), scope);
				if (node.numValidNodes == 2)
				{
					var typeArgsListNode = node.NodeAt(1);
					if (typeArgsListNode != null && typeArgsListNode.numValidNodes > 0)
					{
						bool isUnboundType = typeArgsListNode.RuleName == "unboundTypeRank";
						var numTypeArgs = isUnboundType ? typeArgsListNode.numValidNodes - 1 : typeArgsListNode.numValidNodes / 2;
						var typeDefinition = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArgs) as TypeDefinition;
						if (typeDefinition == null)
							return node.ChildAt(0).resolvedSymbol;

						if (!isUnboundType)
						{
							var typeArgs = new SymbolReference[numTypeArgs];
							for (var i = 0; i < numTypeArgs; ++i)
								typeArgs[i] = new SymbolReference(typeArgsListNode.ChildAt(1 + 2 * i));
							if (typeDefinition.typeParameters != null)
							{
								var constructedType = typeDefinition.ConstructType(typeArgs);
								node.ChildAt(0).resolvedSymbol = constructedType;
								return constructedType;
							}
						}

						return typeDefinition;
					}
				}
				else if (scope is AttributesScope && node.parent.parent.parent.RuleName == "attribute")
				{
					var lastLeaf = node.LeafAt(0);
					if (asMemberOf != null)
						asMemberOf.ResolveAttributeMember(lastLeaf, scope);
					else
						scope.ResolveAttribute(lastLeaf);

					if (lastLeaf.resolvedSymbol == null)
						lastLeaf.resolvedSymbol = unknownSymbol;
					return lastLeaf.resolvedSymbol;
				}
				return ResolveNode(node.ChildAt(0), scope, asMemberOf);

			case "namespaceName":
				return ResolveNode(node.ChildAt(0), scope, asMemberOf) as NamespaceDefinition;

			case "namespaceOrTypeName":
				part = ResolveNode(node.ChildAt(0), scope, null, node.numValidNodes == 1 ? numTypeArguments : 0);
				for (var i = 2; i < node.numValidNodes; i += 2)
					part = ResolveNode(node.ChildAt(i), scope, part, i == node.numValidNodes - 1 ? numTypeArguments : 0);
				return part;

			case "usingAliasDirective":
				return ResolveNode(node.ChildAt(0), scope);

			case "qualifiedIdentifier":
				part = ResolveNode(node.ChildAt(0), scope) as NamespaceDefinition;
				for (var i = 2; part != null && i < node.numValidNodes; i += 2)
				{
					part = ResolveNode(node.ChildAt(i), scope, part);
					var idNode = node.NodeAt(i);
					if (idNode != null && idNode.numValidNodes == 1)
						idNode.ChildAt(0).resolvedSymbol = part;
				}
				return part;

			case "memberInitializer":
				                              // memberInitializarList
				                                     // objectInitializer
				                                            // objectOrCollectionInitializer
				                                                   // objectCreationExpression
				var objectCreationNode = node.parent.parent.parent.parent;
				var constructorNode = objectCreationNode.FindPreviousNode();
				typeNodeType = (ResolveNode(constructorNode) ?? unknownType).TypeOf() as TypeDefinitionBase;

				ResolveNode(node.ChildAt(0), scope, typeNodeType);
				if (node.numValidNodes == 3)
					ResolveNode(node.ChildAt(2), scope);
				return null;

			case "primaryExpression":
				for (var i = 0; i < node.numValidNodes; ++i)
				{
					asMemberOf = part;
					var child = node.ChildAt(i);
					var methodNameNode = child as ParseTree.Node;

					if (i == 0 && child is ParseTree.Leaf) // the "new" keyword
					{
						methodNameNode = node.NodeAt(1);
						if (methodNameNode != null && methodNameNode.numValidNodes > 0)
						{
							var nonArrayTypeNode = methodNameNode.RuleName == "nonArrayType" ? methodNameNode : null;
							if (nonArrayTypeNode != null)
							{
								asMemberOf = ResolveNode(nonArrayTypeNode, scope);
								var node3 = node.NodeAt(2);
								if (node3 != null && node3.RuleName == "objectCreationExpression")
								{
									i += 2;
									part = ResolveNodeAsConstructor(node3, scope, asMemberOf);
									if (part != null && part.kind == SymbolKind.Constructor)
									{
										var asMemberOfAsConstructedType = asMemberOf as ConstructedTypeDefinition;
										if (asMemberOfAsConstructedType != null)
											part = asMemberOfAsConstructedType.GetConstructedMember(part);
									}
								}
								else if (node3 != null) // && node3.RuleName == "arrayCreationExpression")
								{
									i += 2;
									part = ResolveNode(node.ChildAt(i), scope, asMemberOf);
								}
							}
							else // methodNameNode is implicitArrayCreationExpression, or anonymousObjectCreationExpression
								part = ResolveNode(methodNameNode, scope);
						}
					}
					else
					{
						// child is primaryExpressionStart, primaryExpressionPart, or anonymousMethodExpression
						part = ResolveNode(child, scope, asMemberOf);
					}
					if (part == null)
						break;

					SymbolDefinition method = part.kind == SymbolKind.Method || part.kind == SymbolKind.Constructor ? part : null;
					if (part.kind == SymbolKind.MethodGroup && ++i < node.numValidNodes)
					{
						methodNameNode = node.NodeAt(i - 1);
						child = node.ChildAt(i);
						part = ResolveNode(child, scope, part);
						if (part != null)
							method = part.kind == SymbolKind.Method ? part : null;
					}
					if (part == null)
						break;
				
					if (method != null)
					{
//						var asMemberOfConstructedType = asMemberOf as ConstructedTypeDefinition;
//						if (asMemberOfConstructedType != null)
//							part = asMemberOfConstructedType.GetConstructedMember(method);

						if (methodNameNode != null)
						{
//							if (methodNameNode.RuleName == "nonArrayType")
//							{
//								methodNameNode = methodNameNode.NodeAt(0);
//							}

							if (methodNameNode.RuleName == "primaryExpressionStart")
							{
								var methodNameLeaf = methodNameNode.LeafAt(methodNameNode.numValidNodes < 3 ? 0 : 2);
								if (methodNameLeaf != null)
									methodNameLeaf.resolvedSymbol = part;
							}
							else if (methodNameNode.RuleName == "primaryExpressionPart")
							{
								var accessIdentifierNode = methodNameNode.NodeAt(0);
								if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
								{
									var methodNameLeaf = accessIdentifierNode.LeafAt(1);
									if (methodNameLeaf != null)
										methodNameLeaf.resolvedSymbol = part;
								}
							}
//							else if (methodNameNode.RuleName == "nonArrayType")
//							{
//								var nameNode = methodNameNode.ChildAt(0);
//								while (nameNode is ParseTree.Node)
//								{
//									var nameNodeAsNode = nameNode as ParseTree.Node;
//									if (nameNodeAsNode.RuleName == "namespaceOrTypeName")
//										nameNode = nameNodeAsNode.ChildAt(-1);
//									else
//										nameNode = nameNodeAsNode.ChildAt(0);
//								}
//								nameNode.resolvedSymbol = method;
//							}
						}
						else
						{
							node.ChildAt(i).resolvedSymbol = method;
						}
					}
					if (part == null)
						break;

//					if (part.kind == SymbolKind.Method)
//					{
//						var returnType = (part = part.TypeOf()) as TypeDefinitionBase;
//						if (returnType != null)
//							part = returnType.GetThisInstance();
//					}
					if (part.kind == SymbolKind.Constructor)
						part = ((TypeDefinitionBase) part.parentSymbol).GetThisInstance();

					if (part == null)
						break;
				}
				return part;

			case "primaryExpressionStart":
				if (node.numValidNodes == 1 || node.numValidNodes == 2)
					return ResolveNode(node.ChildAt(0), scope, null);
				// else => IDENTIFIER::IDENTIFIER
				part = ResolveNode(node.ChildAt(0), scope, null);
				if (part != null)
					return ResolveNode(node.ChildAt(2), scope, part);
				break;

			case "primaryExpressionPart":
				if (asMemberOf == null)
					asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
				if (asMemberOf != null)
					return ResolveNode(node.ChildAt(0), scope, asMemberOf);
				break;

			case "brackets":
				if (asMemberOf == null)
					asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
				if (asMemberOf != null)
				{
				//	Debug.LogWarning("Resolving brackets on " + asMemberOf.GetTooltipText());
					var arrayType = asMemberOf.TypeOf() as ArrayTypeDefinition;
					if (arrayType != null && arrayType.elementType != null)
					{
					//	UnityEngine.Debug.Log("    elementType " + arrayType.elementType.TypeOf());
						return arrayType.elementType.GetThisInstance();
					}
					if (node.numValidNodes == 3)
					{
						var expressionListNode = node.NodeAt(1);
						if (expressionListNode != null && expressionListNode.numValidNodes >= 1)
						{
							var argumentTypes = new TypeDefinitionBase[(expressionListNode.numValidNodes + 1) / 2];
							for (var i = 0; i < argumentTypes.Length; ++i)
							{
								var expression = ResolveNode(expressionListNode.ChildAt(i*2), scope);
								if (expression == null)
									goto default;
								argumentTypes[i] = expression.TypeOf() as TypeDefinitionBase;
							}
							var indexer = asMemberOf.TypeOf().GetIndexer(argumentTypes);
							if (indexer != null)
								return ((TypeDefinitionBase) indexer.TypeOf()).GetThisInstance();
							else
								return unknownSymbol;
						}
					}
				}
				break;

			case "accessIdentifier":
				if (asMemberOf == null)
					asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
				if (node.numValidNodes == 2)
					return ResolveNode(node.ChildAt(1), scope, asMemberOf);
				if (node.numValidNodes == 3)
				{
					var typeArgsNode = node.NodeAt(2);
					if (typeArgsNode != null && typeArgsNode.RuleName == "typeArgumentList")
						numTypeArguments = typeArgsNode.numValidNodes / 2;
					return ResolveNode(node.ChildAt(1), scope, asMemberOf, numTypeArguments);
				}
				return asMemberOf; // HACK

			case "arguments":
				if (asMemberOf == null)
				{
					var prevNode = node.FindPreviousNode();
					asMemberOf = ResolveNode(prevNode, scope);
					if (asMemberOf == null)
					{
						asMemberOf = ResolveNodeAsExtensionMethod(prevNode, scope);
						if (asMemberOf == null)
							return null;
					}
				}
//				if (node.numValidNodes < 2)
//					return null;

				var argumentListNode = node.numValidNodes >= 2 ? node.NodeAt(1) : null;
				if (argumentListNode != null)
					ResolveNode(argumentListNode, scope);
				
				if (node.parent.RuleName == "attribute")
					return unknownSymbol;

				var methodGroup = asMemberOf as MethodGroupDefinition;
				if (methodGroup != null)
				{
					asMemberOf = methodGroup.ResolveMethodOverloads(argumentListNode, scope);
					var method = asMemberOf as MethodDefinition;
					if (method != null)
					{
						var prevNode = node.FindPreviousNode() as ParseTree.Node;
						var idLeaf = prevNode.LeafAt(0) ?? prevNode.NodeAt(0).LeafAt(1);
						if (method.kind == SymbolKind.Error)
						{
							idLeaf.resolvedSymbol = methodGroup;
							idLeaf.semanticError = method.name;
						}
						else
						{
							idLeaf.resolvedSymbol = method;
						}
						
						var returnType = method.ReturnType();
						return returnType != null ? returnType.GetThisInstance() : null;
					}
				}
				else if (asMemberOf.kind == SymbolKind.MethodGroup)
				{
					var constructedMethodGroup = asMemberOf as ConstructedSymbolDefinition;
					if (constructedMethodGroup != null)
						asMemberOf = constructedMethodGroup.ResolveMethodOverloads(argumentListNode, scope);
					var method = asMemberOf as MethodDefinition;
					if (method != null)
					{
						var prevNode = node.FindPreviousNode() as ParseTree.Node;
						var idLeaf = prevNode.LeafAt(0) ?? prevNode.NodeAt(0).LeafAt(1);
						if (method.kind == SymbolKind.Error)
						{
							idLeaf.resolvedSymbol = methodGroup;
							idLeaf.semanticError = method.name;
						}
						else
						{
							idLeaf.resolvedSymbol = method;
						}
						
						var returnType = method.ReturnType();
						return returnType != null ? returnType.GetThisInstance() : null;
					}
				}
				else if (asMemberOf.kind != SymbolKind.Method && asMemberOf.kind != SymbolKind.Error)
				{
					var typeOf = asMemberOf.TypeOf() as TypeDefinitionBase;
					if (typeOf.kind == SymbolKind.Error)
						return unknownType;

					var returnType = asMemberOf.kind == SymbolKind.Delegate ? typeOf :

					typeOf.kind == SymbolKind.Delegate ? typeOf.TypeOf() as TypeDefinitionBase : null;
					if (returnType != null)
						return returnType.GetThisInstance();
					
				//	Debug.Log(asMemberOf.GetTooltipText());
//					if (asMemberOf.kind != SymbolKind.Event)
					node.LeafAt(0).semanticError = "Cannot invoke " + asMemberOf.GetName();
				}
				
				return asMemberOf;

			case "argument":
				if (node.numValidNodes == 1)
					dummy = ResolveNode(node.ChildAt(0), scope);
				else if (node.numValidNodes == 3)
					dummy = ResolveNode(node.ChildAt(2), scope);
				return dummy;

			case "argumentList":
				for (var i = 0; i < node.numValidNodes; i += 2)
					dummy = ResolveNode(node.ChildAt(i), scope);
				return dummy;

			case "argumentValue":
				return ResolveNode(node.ChildAt(-1), scope);

			case "argumentName":
				//return ResolveNode(node.ChildAt(0), asMemberOf: asMemberOf);
				                                       // arguments
				                                // argumentList
				                         // argument
				var parameterNameLeaf = node.LeafAt(0);
				if (parameterNameLeaf == null)
					return unknownSymbol;
				var argumentsNode = node.parent.parent.parent;
				var invokableNode = argumentsNode.FindPreviousNode();
				var invokableSymbol = ResolveNode(invokableNode);
				if (invokableSymbol.kind != SymbolKind.MethodGroup)
					invokableSymbol = invokableSymbol.parentSymbol;
				methodGroup = invokableSymbol as MethodGroupDefinition;
				if (methodGroup == null)
					return parameterNameLeaf.resolvedSymbol = unknownSymbol;
				return methodGroup.ResolveParameterName(parameterNameLeaf);

		case "castExpression":
			if (node.numValidNodes == 4)
				ResolveNode(node.ChildAt(3), scope);
			var castType = ResolveNode(node.ChildAt(1), scope) as TypeDefinitionBase;
				if (castType != null)
					return castType.GetThisInstance();
				break;

			case "typeofExpression":
				if (node.numValidNodes >= 3)
					ResolveNode(node.ChildAt(2), scope);
				return ((TypeDefinitionBase) ReflectedTypeReference.ForType(typeof(Type)).definition).GetThisInstance();
				//var tempAssemblyDefinition = AssemblyDefinition.FromAssembly(typeof(System.Type).Assembly);
				//return tempAssemblyDefinition.FindNamespace("System").FindName("Type");

			case "defaultValueExpression":
				if (node.numValidNodes >= 3)
				{
					var typeNode = ResolveNode(node.ChildAt(2), scope) as TypeDefinitionBase;
					if (typeNode != null)
						return typeNode.GetThisInstance();
				}
				break;

			case "sizeofExpression":
				if (node.numValidNodes >= 3)
					ResolveNode(node.ChildAt(2), scope);
				return builtInTypes["int"].GetThisInstance();

			case "localVariableInitializer":
			case "variableReference":
			case "expression":
			case "constantExpression":
			case "nonAssignmentExpression":
				return ResolveNode(node.ChildAt(0), scope);

			case "parenExpression":
				return ResolveNode(node.ChildAt(1), scope);

			case "nullCoalescingExpression":
				for (var i = 2; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope); // HACK
				return ResolveNode(node.ChildAt(0), scope); // HACK

			case "conditionalExpression":
				if (node.numValidNodes >= 3)
				{
					ResolveNode(node.ChildAt(0), scope);
					if (node.numValidNodes == 5)
						ResolveNode(node.ChildAt(4), scope);
					return ResolveNode(node.ChildAt(2), scope, null); // HACK
				}
				else
					return ResolveNode(node.ChildAt(0), scope, asMemberOf);

			case "unaryExpression":
				if (node.numValidNodes == 1)
					return ResolveNode(node.ChildAt(0), scope, null);
				if (node.ChildAt(0) is ParseTree.Node)
					return ResolveNode(node.ChildAt(0), scope, null);
				return ResolveNode(node.ChildAt(1), scope, null);

			case "inclusiveOrExpression":
			case "exclusiveOrExpression":
			case "andExpression":
			case "shiftExpression":
			case "additiveExpression":
			case "multiplicativeExpression":
				for (var i = 2; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				return ResolveNode(node.ChildAt(0), scope); // HACK

			case "arrayCreationExpression":
				if (asMemberOf == null)
					asMemberOf = ResolveNode(node.FindPreviousNode());
				var resultType = asMemberOf as TypeDefinitionBase;
				if (resultType == null)
					return unknownType.MakeArrayType(1);

				var rankSpecifiersNode = node.FindChildByName("rankSpecifiers") as ParseTree.Node;
				if (rankSpecifiersNode == null || rankSpecifiersNode.childIndex > 0)
				{
					var expressionListNode = node.NodeAt(1);
					if (expressionListNode != null && expressionListNode.RuleName == "expressionList")
						resultType = resultType.MakeArrayType(1 + expressionListNode.numValidNodes / 2);
				}
				if (rankSpecifiersNode != null && rankSpecifiersNode.numValidNodes != 0)
				{
					for (var i = 1; i < rankSpecifiersNode.numValidNodes; i += 2)
					{
						rank = 1;
						while (((FGGrammar.Lit) rankSpecifiersNode.ChildAt(i).grammarNode).body[0] == ',')
						{
							++rank;
							++i;
						}
						resultType = resultType.MakeArrayType(rank);
					}
				}

				var initializerNode = node.NodeAt(-1);
				if (initializerNode != null && initializerNode.RuleName == "arrayInitializer")
					ResolveNode(initializerNode);

				return (resultType ?? unknownType).GetThisInstance();

			case "implicitArrayCreationExpression":
				resultType = null;

				var rankSpecifierNode = node.NodeAt(0);
				rank = rankSpecifierNode != null && rankSpecifierNode.numValidNodes > 0 ? rankSpecifierNode.numValidNodes - 1 : 1;

				initializerNode = node.NodeAt(1);
				var elements = initializerNode != null ? ResolveNode(initializerNode) : null;
				if (elements != null)
					resultType = (elements.TypeOf() as TypeDefinitionBase ?? unknownType).MakeArrayType(rank);

				return (resultType ?? unknownType).GetThisInstance();

			case "arrayInitializer":
				if (node.numValidNodes >= 2)
					return ResolveNode(node.ChildAt(1), scope);
				break;

			case "variableInitializerList":
				TypeDefinitionBase commonType = null;
				for (var i = 0; i < node.numValidNodes; i += 2)
				{
					var type = (ResolveNode(node.ChildAt(i), scope) ?? unknownSymbol).TypeOf() as TypeDefinitionBase;
					if (type != null)
					{
						if (commonType == null)
						{
							commonType = type;
						}
						else
						{
							// HACK!!!
							if (commonType.DerivesFrom(type))
								commonType = type;
						}
					}
				}
				return commonType;

			case "variableInitializer":
				return ResolveNode(node.ChildAt(0), scope);

			case "conditionalOrExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "conditionalAndExpression";
				}
				for (var i = 0; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				return builtInTypes["bool"]; // HACK

			case "conditionalAndExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "inclusiveOrExpression";
				}
				for (var i = 0; i < node.numValidNodes; i += 2)
					ResolveNode(node.ChildAt(i), scope);
				return builtInTypes["bool"]; // HACK

			case "equalityExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "relationalExpression";
				}
				for (var i = 0; i < node.numValidNodes; i += 2 )
					ResolveNode(node.ChildAt(i), scope);
				return builtInTypes["bool"]; // HACK

			case "relationalExpression":
				if (node.numValidNodes == 1)
				{
					node = node.NodeAt(0);
					goto case "shiftExpression";
				}
				part = ResolveNode(node.ChildAt(0), scope);
				for (var i = 2; i < node.numValidNodes; i += 2)
				{
					if (node.ChildAt(i - 1).IsLit("as"))
					{
						part = ResolveNode(node.ChildAt(i), scope);
						if (part is TypeDefinitionBase)
							part = (part as TypeDefinitionBase).GetThisInstance();
					}
					else
					{
						ResolveNode(node.ChildAt(i), scope);
						part = builtInTypes["bool"].GetThisInstance(); // HACK
					}
				}
				return part;

			case "booleanExpression":
				ResolveNode(node.ChildAt(0), scope);
				return builtInTypes["bool"];

			case "lambdaExpression":
				ResolveNode(node.ChildAt(0), scope);
				if (node.numValidNodes == 3)
					return ResolveNode(node.ChildAt(2), scope);
				return unknownSymbol;

			case "lambdaExpressionBody":
				var expressionNode = node.NodeAt(0);
				if (expressionNode != null)
					return ResolveNode(expressionNode);
				return null;

			case "objectCreationExpression":
				var objectType = (ResolveNode(node.FindPreviousNode(), scope) ?? unknownType).TypeOf() as TypeDefinitionBase;
				return objectType != null ? objectType.GetThisInstance() : null;

			case "classMemberDeclaration":
				return null;

			case "implicitAnonymousFunctionParameterList":
			case "implicitAnonymousFunctionParameter":
			case "explicitAnonymousFunctionSignature":
			case "explicitAnonymousFunctionParameterList":
			case "explicitAnonymousFunctionParameter":
			case "anonymousFunctionSignature":
			case "qid":
			case "qidStart":
			case "qidPart":
			case "typeParameterList":
			case "constructorInitializer":
			case "interfaceMemberDeclaration":
			case "collectionInitializer":
			case "elementInitializerList":
			case "elementInitializer":
			case "methodHeader":
				return null;

			default:
		//		if (missingResolveNodePaths.Add(node.RuleName))
		//			UnityEngine.Debug.Log("TODO: Add ResolveNode path for " + node.RuleName);
				return null;
		}

	//	Debug.Log("TODO: Canceled ResolveNode for " + node.RuleName);
		return null;
	}

	protected virtual SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
	{
		return null;
	}

	public virtual SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
	{
		memberName = DecodeId(memberName);
		
		SymbolDefinition definition;
		if (!members.TryGetValue(numTypeParameters > 0 ? memberName + "`" + numTypeParameters : memberName, out definition))
		{
			var marker = memberName.IndexOf('`');
			if (marker > 0)
			{
				Debug.LogError("FindName!!! " + memberName);
				members.TryGetValue(memberName.Substring(0, marker), out definition);
			}
		}
		if (asTypeOnly && definition != null && definition.kind != SymbolKind.Namespace && !(definition is TypeDefinitionBase))
			return null;
		return definition;
	}

	public virtual void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, AssemblyDefinition assembly)
	{
		GetMembersCompletionData(data, fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any, assembly);
	//	base.GetCompletionData(data, assembly);
	}

	public virtual void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, AssemblyDefinition assembly)
	{
		if ((mask & AccessLevelMask.Public) != 0)
		{
			if (assembly.InternalsVisibleIn(this.Assembly))
				mask |= AccessLevelMask.Internal;
			else
				mask &= ~AccessLevelMask.Internal;
		}
		
		flags = flags & (BindingFlags.Static | BindingFlags.Instance);
		bool onlyStatic = flags == BindingFlags.Static;
		bool onlyInstance = flags == BindingFlags.Instance;
		
		if (!onlyInstance)
		{
			var tp = GetTypeParameters();
			if (tp != null)
			{
				for (var i = 0; i < tp.Count; ++i)
				{
					TypeParameterDefinition p = tp[i];
					if (!data.ContainsKey(p.name))
						data.Add(p.name, p);
				}
			}
		}

		foreach (var m in members)
		{
			if (m.Value.kind == SymbolKind.Namespace)
			{
				if (!data.ContainsKey(m.Key))
					data.Add(m.Key, m.Value);
			}
			else if (m.Value.kind != SymbolKind.MethodGroup)
			{
				if ((onlyStatic ? !m.Value.IsInstanceMember : onlyInstance ? m.Value.IsInstanceMember : true)
					&& m.Value.IsAccessible(mask)
					&& m.Value.kind != SymbolKind.Constructor && m.Value.kind != SymbolKind.Destructor && m.Value.kind != SymbolKind.Indexer
					&& !data.ContainsKey(m.Key))
				{
					data.Add(m.Key, m.Value);
				}
			}
			else
			{
				var methodGroup = m.Value as MethodGroupDefinition;
				foreach (var method in methodGroup.methods)
					if ((onlyStatic ? method.IsStatic : onlyInstance ? !method.IsStatic : true)
						&& method.IsAccessible(mask)
						&& method.kind != SymbolKind.Constructor && method.kind != SymbolKind.Destructor && method.kind != SymbolKind.Indexer
						&& !data.ContainsKey(m.Key))
					{
						data.Add(m.Key, method);
					}
			}
		}
	}
	
	public bool IsInstanceMember
	{
		get
		{
			return !IsStatic && kind != SymbolKind.ConstantField && !(this is TypeDefinitionBase);
		}
	}

	public virtual bool IsStatic
	{
		get
		{
			return (modifiers & Modifiers.Static) != 0;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Static;
			else
				modifiers &= ~Modifiers.Static;
		}
	}

	public bool IsPublic
	{
		get
		{
			return (modifiers & Modifiers.Public) != 0 ||
				(kind == SymbolKind.Namespace) ||
				parentSymbol != null && (
					parentSymbol.parentSymbol != null
					&& (kind == SymbolKind.Method || kind == SymbolKind.Indexer)
					&& (parentSymbol.parentSymbol.kind == SymbolKind.Interface)
					||
					(kind == SymbolKind.Property || kind == SymbolKind.Event)
					&& (parentSymbol.kind == SymbolKind.Interface)
				);
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Public;
			else
				modifiers &= ~Modifiers.Public;
		}
	}

	public bool IsInternal
	{
		get
		{
			return (modifiers & Modifiers.Internal) != 0;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Internal;
			else
				modifiers &= ~Modifiers.Internal;
		}
	}

	public bool IsProtected
	{
		get
		{
			return (modifiers & Modifiers.Protected) != 0;
		}
		set
		{
			if (value)
				modifiers |= Modifiers.Protected;
			else
				modifiers &= ~Modifiers.Protected;
		}
	}

	//public virtual bool IsGeneric
	//{
	//	get
	//	{
	//		return false;
	//	}
	//}

	public AssemblyDefinition Assembly
	{
		get
		{
			var assembly = this;
			while (assembly != null)
			{
				var result = assembly as AssemblyDefinition;
				if (result != null)
					return result;
				assembly = assembly.parentSymbol;
			}
			return null;
		}
	}

	public virtual bool IsSameType(TypeDefinitionBase type)
	{
		return type == this;
	}

	public bool IsSameOrParentOf(TypeDefinitionBase type)
	{
		while (type != null)
		{
			if (type == this)
				return true;
			type = type.parentSymbol as TypeDefinitionBase;
		}
		return false;
	}

	public virtual TypeDefinitionBase TypeOfTypeParameter(TypeParameterDefinition tp)
	{
		if (parentSymbol != null)
			return parentSymbol.TypeOfTypeParameter(tp);
		return tp;
	}

	public virtual bool IsAccessible(AccessLevelMask accessLevelMask)
	{
		if (accessLevelMask == AccessLevelMask.None)
			return false;
		if (IsPublic)
			return true;
		if (IsProtected && (accessLevelMask & AccessLevelMask.Protected) != 0)
			return true;
		if (IsInternal && (accessLevelMask & AccessLevelMask.Internal) != 0)
			return true;

		return (accessLevelMask & AccessLevelMask.Private) != 0;
	}
}

static class DictExtensions
{
	public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
	{
		return "{" + string.Join(",", dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray()) + "}";
	}
}

public class SymbolDeclaration //: IVisitableTreeNode<SymbolDeclaration, SymbolDeclaration>
{
	public SymbolDefinition definition;
	public Scope scope;

	public SymbolKind kind;

	public ParseTree.Node parseTreeNode;
	public Modifiers modifiers;
	public int numTypeParameters;

	protected string name;

	//public SymbolDeclaration parentDeclaration;
	//public List<SymbolDeclaration> nestedDeclarations = new List<SymbolDeclaration>();

	public SymbolDeclaration() {}

	public SymbolDeclaration(string name)
	{
		this.name = name;
	}

	public bool IsValid()
	{
		var node = parseTreeNode;
		while (node != null && node.RuleName != "compilationUnit")
			node = node.parent;
		if (node != null)
			return true;

		if (scope != null)
		{
			scope.RemoveDeclaration(this);
		}
		else if (definition != null)
		{
			Debug.Log("Scope is null for declaration " + name + ". Removing " + definition);
			if (definition.parentSymbol != null)
				definition.parentSymbol.RemoveDeclaration(this);
		}
		scope = null;
		return false;
	}

	public ParseTree.BaseNode NameNode()
	{
		if (parseTreeNode == null || parseTreeNode.numValidNodes == 0)
			return null;

		ParseTree.BaseNode nameNode = null;
		switch (parseTreeNode.RuleName)
		{
			case "namespaceDeclaration":
				nameNode = parseTreeNode.ChildAt(1);
				var nameNodeAsNode = nameNode as ParseTree.Node;
				if (nameNodeAsNode != null && nameNodeAsNode.numValidNodes != 0)
					nameNode = nameNodeAsNode.ChildAt(-1) ?? nameNode;
				break;

			case "interfaceDeclaration":
			case "structDeclaration":
			case "classDeclaration":
			case "enumDeclaration":
				nameNode = parseTreeNode.ChildAt(1);
				break;

			case "delegateDeclaration":
				nameNode = parseTreeNode.ChildAt(2);
				break;

			case "eventDeclarator":
			case "eventWithAccessorsDeclaration":
			case "propertyDeclaration":
			case "interfacePropertyDeclaration":
			case "variableDeclarator":
			case "localVariableDeclarator":
			case "constantDeclarator":
			case "interfaceMethodDeclaration":
			case "catchExceptionIdentifier":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "methodDeclaration":
				var methodHeaderNode = parseTreeNode.NodeAt(0);
				if (methodHeaderNode != null && methodHeaderNode.numValidNodes > 0)
					nameNode = methodHeaderNode.ChildAt(0);
				break;

			case "methodHeader":
			case "constructorDeclarator":
			case "destructorDeclarator":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "fixedParameter":
			case "parameterArray":
			case "explicitAnonymousFunctionParameter":
				nameNode = parseTreeNode.FindChildByName("NAME");
				break;

			case "implicitAnonymousFunctionParameter":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "typeParameter":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "enumMemberDeclaration":
				if (parseTreeNode.ChildAt(0) is ParseTree.Node)
					nameNode = parseTreeNode.ChildAt(1);
				else
					nameNode = parseTreeNode.ChildAt(0);
				break;

			case "statementList":
			case "lambdaExpression":
			case "anonymousMethodExpression":
				return null;

			case "interfaceTypeList":
				nameNode = parseTreeNode.ChildAt(0);
				break;

			case "foreachStatement":
			case "fromClause":
				nameNode = parseTreeNode.FindChildByName("NAME");
				break;

			case "getAccessorDeclaration":
			case "interfaceGetAccessorDeclaration":
			case "setAccessorDeclaration":
			case "interfaceSetAccessorDeclaration":
			case "addAccessorDeclaration":
			case "removeAccessorDeclaration":
				nameNode = parseTreeNode.FindChildByName("IDENTIFIER");
				break;

			case "indexerDeclaration":
			case "labeledStatement":
				return parseTreeNode.ChildAt(0);

			case "conversionOperatorDeclarator":
			case "operatorDeclarator":
				return null;

			default:
				Debug.LogWarning("Don't know how to extract symbol name from: " + parseTreeNode);
				return null;
		}
		return nameNode;
	}

	public string Name
	{
		get
		{
			if (name != null)
				return name;

			if (definition != null)
				return name = definition.name;

			if (kind == SymbolKind.Constructor)
				return name = ".ctor";
			if (kind == SymbolKind.Indexer)
				return name = "Item";
			if (kind == SymbolKind.LambdaExpression)
			{
				var cuNode = parseTreeNode;
				while (cuNode != null && !(cuNode.scope is CompilationUnitScope))
					cuNode = cuNode.parent;
				name = cuNode != null ? cuNode.scope.CreateAnonymousName() : scope.CreateAnonymousName();
				return name;
			}
			if (kind == SymbolKind.Accessor)
			{
				switch (parseTreeNode.RuleName)
				{
					case "getAccessorDeclaration":
					case "interfaceGetAccessorDeclaration":
						return "get";
					case "setAccessorDeclaration":
					case "interfaceSetAccessorDeclaration":
						return "set";
					case "addAccessorDeclaration":
						return "add";
					case "removeAccessorDeclaration":
						return "remove";
				}
			}

			var nameNode = NameNode();
			var asNode = nameNode as ParseTree.Node;
			if (asNode != null && asNode.numValidNodes != 0 && asNode.RuleName == "memberName")
			{
				asNode = asNode.NodeAt(0);
				if (asNode != null && asNode.numValidNodes != 0 && asNode.RuleName == "qid")
				{
					asNode = asNode.NodeAt(-1);
					if (asNode != null && asNode.numValidNodes != 0)
					{
						if (asNode.RuleName == "qidStart")
						{
							nameNode = asNode.ChildAt(0);
						}
						else
						{
							asNode = asNode.NodeAt(0);
							if (asNode != null && asNode.numValidNodes != 0)
							{
								nameNode = asNode.ChildAt(1);
							}
						}
					}
				}
			}
			name = nameNode != null ? nameNode.Print() : "UNKNOWN";
			return name;
		}
	}
	
	public string ReflectionName {
		get {
			if (numTypeParameters == 0)
				return Name;
			return Name + '`' + numTypeParameters;
		}
	}

	//public bool Accept(IHierarchicalVisitor<SymbolDeclaration, SymbolDeclaration> visitor)
	//{
	//    if (nestedDeclarations.Count == 0)
	//        return visitor.Visit(this);
		
	//    if (visitor.VisitEnter(this))
	//    {
	//        foreach (var nested in nestedDeclarations)
	//            if (!nested.Accept(visitor))
	//                break;
	//    }
	//    return visitor.VisitLeave(this);
	//}

	public override string ToString()
	{
		var sb = new StringBuilder();
		Dump(sb, string.Empty);
		return sb.ToString();
	}

	protected virtual void Dump(StringBuilder sb, string indent)
	{
		sb.AppendLine(indent + kind + " " + ReflectionName + " (" + GetType() + ")");
		
		//foreach (var nested in nestedDeclarations)
		//    nested.Dump(sb, indent + "  ");
	}

	public bool HasAllModifiers(Modifiers mods)
	{
		return (modifiers & mods) == mods;
	}

	public bool HasAnyModifierOf(Modifiers mods)
	{
		return (modifiers & mods) != 0;
	}
}

public class NamespaceDeclaration : SymbolDeclaration
{
	public Dictionary<string, SymbolReference> importedNamespaces = new Dictionary<string, SymbolReference>();
	public Dictionary<string, SymbolReference> typeAliases = new Dictionary<string, SymbolReference>();

	public NamespaceDeclaration(string nsName)
		: base(nsName)
	{}

	public NamespaceDeclaration() {}

	public void ImportNamespace(string namespaceToImport, ParseTree.BaseNode declaringNode)
	{
		throw new NotImplementedException ();
	}

	protected override void Dump(StringBuilder sb, string indent)
	{
		base.Dump(sb, indent);

		sb.AppendLine(indent + "Imports:");
		var indent2 = indent + "  ";
		foreach (var ns in importedNamespaces)
			sb.AppendLine(indent2 + ns);

		sb.AppendLine("  Aliases:");
		foreach (var ta in typeAliases)
			sb.AppendLine(indent2 + ta);
	}
}

public class CompilationUnitScope : NamespaceScope
{
	public string path;

	public AssemblyDefinition assembly;

	private int numAnonymousSymbols;
	
	public CompilationUnitScope() : base(null) {}

	public override string CreateAnonymousName()
	{
		return ".Anonymous_" + numAnonymousSymbols++;
	}
}

public class AssemblyDefinition : SymbolDefinition
{
	public enum UnityAssembly
	{
		None,
		DllFirstPass,
		CSharpFirstPass,
		UnityScriptFirstPass,
		BooFirstPass,
		DllEditorFirstPass,
		CSharpEditorFirstPass,
		UnityScriptEditorFirstPass,
		BooEditorFirstPass,
		Dll,
		CSharp,
		UnityScript,
		Boo,
		DllEditor,
		CSharpEditor,
		UnityScriptEditor,
		BooEditor,

		Last = BooEditor
	}

//	static readonly string projectAssembliesPath =
//		UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.Length - "Assets".Length) + "Library/ScriptAssemblies/";

	public readonly Assembly assembly;
	public readonly UnityAssembly assemblyId;
	//public readonly string assemblyPath;
	//public readonly string assemblyName;

	private AssemblyDefinition[] _referencedAssemblies;
	public AssemblyDefinition[] referencedAssemblies
	{
		get {
			if (_referencedAssemblies == null)
			{
				var raSet = new HashSet<AssemblyDefinition>();
				if (assembly != null)
					foreach (var ra in assembly.GetReferencedAssemblies())
					{
						var assemblyDefinition = FromName(ra.Name);
						if (assemblyDefinition != null)
							raSet.Add(assemblyDefinition);
					}
				else
					Debug.LogWarning(this);
				_referencedAssemblies = new AssemblyDefinition[raSet.Count];
				raSet.CopyTo(_referencedAssemblies);
			}
			return _referencedAssemblies;
		}
	}

	public Dictionary<string, CompilationUnitScope> compilationUnits;

	private static readonly Dictionary<Assembly, AssemblyDefinition> allAssemblies = new Dictionary<Assembly, AssemblyDefinition>();
	public static AssemblyDefinition FromAssembly(Assembly assembly)
	{
		AssemblyDefinition definition;
		if (!allAssemblies.TryGetValue(assembly, out definition))
		{
			definition = new AssemblyDefinition(assembly);
			allAssemblies[assembly] = definition;
		}
		return definition;
	}

	private static readonly string[] unityAssemblyNames = new[]
	{
		null,
		"assembly-csharp-firstpass",
		"assembly-unityscript-firstpass",
		"assembly-boo-firstpass",
		null,
		"assembly-csharp-editor-firstpass",
		"assembly-unityscript-editor-firstpass",
		"assembly-boo-editor-firstpass",
		null,
		"assembly-csharp",
		"assembly-unityscript",
		"assembly-boo",
		null,
		"assembly-csharp-editor",
		"assembly-unityscript-editor",
		"assembly-boo-editor"
	};
	
	public static bool IsScriptAssemblyName(string name)
	{
		return Array.IndexOf<string>(unityAssemblyNames, name.ToLowerInvariant()) >= 0;
	}
	
	private static AssemblyDefinition FromName(string assemblyName)
	{
		assemblyName = assemblyName.ToLower();
		var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (var i = domainAssemblies.Length; i-- > 0; )
		{
			var assembly = domainAssemblies[i];
			if (assembly is System.Reflection.Emit.AssemblyBuilder)
				continue;
			if (assembly.GetName().Name.ToLower() == assemblyName)
				return FromAssembly(assembly);
		}
		return null;
	}

	private static readonly AssemblyDefinition[] unityAssemblies = new AssemblyDefinition[(int) UnityAssembly.Last - 1];
	public static AssemblyDefinition FromId(UnityAssembly assemblyId)
	{
		var index = ((int) assemblyId) - 1;
		if (unityAssemblies[index] == null)
		{
			var assemblyName = unityAssemblyNames[index];
			unityAssemblies[index] = FromName(assemblyName) ?? new AssemblyDefinition(assemblyId);
		}
		return unityAssemblies[index];
	}

	public static AssemblyDefinition FromAssetPath(string pathName)
	{
		var ext = (System.IO.Path.GetExtension(pathName) ?? string.Empty).ToLower();
		var isCSharp = ext == ".cs";
		var isUnityScript = ext == ".js";
		var isBoo = ext == ".boo";
		if (!isCSharp && !isUnityScript && !isBoo)
			return null;

		var path = (System.IO.Path.GetDirectoryName(pathName) ?? string.Empty).ToLower() + "/";

		//var isIgnoredScript = false; // TODO: Implement this!
		//if (isIgnoredScript)
		//    return null;

		var isPlugins = path.StartsWith("assets/plugins/");
		var isStandardAssets = path.StartsWith("assets/standardassets/");
		var isEditor = !isPlugins && path.Contains("/editor/") || path.StartsWith("assets/plugins/editor/");
		var isFirstPass = isPlugins || isStandardAssets;

		UnityAssembly assemblyId;
		if (isFirstPass && isEditor)
			assemblyId = isCSharp ? UnityAssembly.CSharpEditorFirstPass : isBoo ? UnityAssembly.BooEditorFirstPass : UnityAssembly.UnityScriptEditorFirstPass;
		else if (isEditor)
			assemblyId = isCSharp ? UnityAssembly.CSharpEditor : isBoo ? UnityAssembly.BooEditor : UnityAssembly.UnityScriptEditor;
		else if (isFirstPass)
			assemblyId = isCSharp ? UnityAssembly.CSharpFirstPass : isBoo ? UnityAssembly.BooFirstPass : UnityAssembly.UnityScriptFirstPass;
		else
			assemblyId = isCSharp ? UnityAssembly.CSharp : isBoo ? UnityAssembly.Boo : UnityAssembly.UnityScript;

		return FromId(assemblyId);
	}

	static AssemblyDefinition()
	{
		var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (var i = domainAssemblies.Length; i-- > 0; )
		{
			var assembly = domainAssemblies[i];
			if (!(assembly is System.Reflection.Emit.AssemblyBuilder))
				FromAssembly(assembly);
		}
	}

	private AssemblyDefinition(UnityAssembly id)
	{
		assemblyId = id;
	}

	private AssemblyDefinition(Assembly assembly)
	{
		this.assembly = assembly;

		switch (assembly.GetName().Name.ToLower())
		{
			case "assembly-csharp-firstpass":
				assemblyId = UnityAssembly.CSharpFirstPass;
				break;
			case "assembly-unityscript-firstpass":
				assemblyId = UnityAssembly.UnityScriptFirstPass;
				break;
			case "assembly-boo-firstpass":
				assemblyId = UnityAssembly.BooFirstPass;
				break;
			case "assembly-csharp-editor-firstpass":
				assemblyId = UnityAssembly.CSharpEditorFirstPass;
				break;
			case "assembly-unityscript-editor-firstpass":
				assemblyId = UnityAssembly.UnityScriptEditorFirstPass;
				break;
			case "assembly-boo-editor-firstpass":
				assemblyId = UnityAssembly.BooEditorFirstPass;
				break;
			case "assembly-csharp":
				assemblyId = UnityAssembly.CSharp;
				break;
			case "assembly-unityscript":
				assemblyId = UnityAssembly.UnityScript;
				break;
			case "assembly-boo":
				assemblyId = UnityAssembly.Boo;
				break;
			case "assembly-csharp-editor":
				assemblyId = UnityAssembly.CSharpEditor;
				break;
			case "assembly-unityscript-editor":
				assemblyId = UnityAssembly.UnityScriptEditor;
				break;
			case "assembly-boo-editor":
				assemblyId = UnityAssembly.BooEditor;
				break;
			default:
				assemblyId = UnityAssembly.None;
				break;
		}
	}

	public string AssemblyName
	{
		get
		{
			return assembly.GetName().Name;
		}
	}
	
	public bool InternalsVisibleIn(AssemblyDefinition referencedAssembly)
	{
		if (referencedAssembly == this)
			return true;
			
		//TODO: Check are internals visible

		return false;
	}

	public static CompilationUnitScope GetCompilationUnitScope(string assetPath, bool forceCreateNew = false)
	{
		assetPath = assetPath.ToLower();

		var assembly = FromAssetPath(assetPath);
		if (assembly == null)
			return null;

		if (assembly.compilationUnits == null)
			assembly.compilationUnits = new Dictionary<string, CompilationUnitScope>();

		CompilationUnitScope scope;
		if (!assembly.compilationUnits.TryGetValue(assetPath, out scope) || forceCreateNew)
		{
			if (forceCreateNew)
			{
				if (scope != null && scope.typeDeclarations != null)
				{
					var scopeTypes = scope.typeDeclarations;
					for (var i = scopeTypes.Count; i --> 0; )
					{
						var typeDeclaration = scopeTypes[i];
						scope.RemoveDeclaration(typeDeclaration);
					}
				}
				assembly.compilationUnits.Remove(assetPath);
			}

			scope = new CompilationUnitScope
			{
				assembly = assembly,
				path = assetPath,
			};
			assembly.compilationUnits[assetPath] = scope;

			//var cuDefinition = new CompilationUnitDefinition
			//{
			//    kind = SymbolKind.None,
			//    parentSymbol = assembly,
			//};

			scope.declaration = new NamespaceDeclaration
			{
				kind = SymbolKind.Namespace,
				definition = assembly.GlobalNamespace,
			};
			scope.definition = assembly.GlobalNamespace;
		}
		return scope;
	}

	//public AssemblyDefinition(UnityAssembly assemblyId)
	//{
	//    this.assemblyId = assemblyId;

	//    if (assemblyId != UnityAssembly.None)
	//        assemblyPath = projectAssembliesPath;

	//    switch (assemblyId)
	//    {
	//        case UnityAssembly.None:
	//        case UnityAssembly.DllFirstPass:
	//        case UnityAssembly.DllEditorFirstPass:
	//        case UnityAssembly.Dll:
	//        case UnityAssembly.DllEditor:
	//            break;

	//        case UnityAssembly.CSharpFirstPass:
	//            assemblyName = "Assembly-CSharp-firstpass.dll";
	//            break;
	//        case UnityAssembly.UnityScriptFirstPass:
	//            assemblyName = "Assembly-UnityScript-firstpass.dll";
	//            break;
	//        case UnityAssembly.BooFirstPass:
	//            assemblyName = "Assembly-Boo-firstpass.dll";
	//            break;
	//        case UnityAssembly.CSharpEditorFirstPass:
	//            assemblyName = "Assembly-CSharp-Editor-firstpass.dll";
	//            break;
	//        case UnityAssembly.UnityScriptEditorFirstPass:
	//            assemblyName = "Assembly-UnityScript-Editor-firstpass.dll";
	//            break;
	//        case UnityAssembly.BooEditorFirstPass:
	//            assemblyName = "Assembly-Boo-Editor-firstpass.dll";
	//            break;
	//        case UnityAssembly.CSharp:
	//            assemblyName = "Assembly-CSharp.dll";
	//            break;
	//        case UnityAssembly.UnityScript:
	//            assemblyName = "Assembly-UnityScript.dll";
	//            break;
	//        case UnityAssembly.Boo:
	//            assemblyName = "Assembly-Boo.dll";
	//            break;
	//        case UnityAssembly.CSharpEditor:
	//            assemblyName = "Assembly-CSharp-Editor.dll";
	//            break;
	//        case UnityAssembly.UnityScriptEditor:
	//            assemblyName = "Assembly-UnityScript-Editor.dll";
	//            break;
	//        case UnityAssembly.BooEditor:
	//            assemblyName = "Assembly-Boo-Editor.dll";
	//            break;
	//    }
	//}

	//public AssemblyDefinition(string filePath, string fileName)
	//{
	//    assemblyPath = filePath;
	//    assemblyName = fileName;
	//}

	private NamespaceDefinition _globalNamespace;
	public NamespaceDefinition GlobalNamespace
	{
		get { return _globalNamespace ?? InitializeGlobalNamespace(); }
		set { _globalNamespace = value; }
	}

	private NamespaceDefinition InitializeGlobalNamespace()
	{
	//	var timer = new Stopwatch();
	//	timer.Start();

		_globalNamespace = new NamespaceDefinition { name = "", kind = SymbolKind.Namespace, parentSymbol = this };

		//assemblyPath = projectAssembliesPath + "Assembly-CSharp-Editor.dll";
		//Debug.Log(mdbPath + " exists: " + System.IO.File.Exists(mdbPath));

		//var readerParameters = new ReaderParameters { ReadSymbols = true };
		//var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
		//foreach (var t in assemblyDefinition.MainModule.Types)
		//{
		//    if (t.HasMethods)
		//    {
		//        foreach (var method in t.Methods)
		//        {
		//            var body = method.Body;
		//            if (body != null && body.Scope != null)
		//            {
		//                var sp = body.Scope.Start.SequencePoint;
		//                Debug.Log(t.Namespace + " " + t.Name + " in " + sp.Document.Url + ":" + sp.StartLine);
		//                break;
		//            }
		//        }
		//    }
		//    else
		//        Debug.Log(t.FullName);
		//    //for (var i = 0; i < symFile.SourceCount; ++i)
		//    //{
		//    //    var source = symFile.Sources[i];
		//    //    Debug.Log(source);
		//    //}
		//}
		////Mono.Cecil.Mdb.MdbReader mdbReader = new Mono.Cecil.Mdb.MdbReader(symFile);

		if (assembly != null)
		{
			var types = assemblyId != UnityAssembly.None ? assembly.GetTypes() : assembly.GetExportedTypes();
			foreach (var t in types)
			{
				if (t.IsNested)
					continue;
	
				SymbolDefinition current = _globalNamespace;
	
				if (!string.IsNullOrEmpty(t.Namespace))
				{
					var ns = t.Namespace.Split('.');
					for (var i = 0; i < ns.Length; ++i)
					{
						var nsName = ns[i];
						var definition = current.FindName(nsName, 0, true);
						if (definition != null)
						{
							current = definition;
						}
						else
						{
							var nsd = new NamespaceDefinition
							{
								kind = SymbolKind.Namespace,
								name = nsName,
								parentSymbol = current,
								accessLevel = AccessLevel.Public,
								modifiers = Modifiers.Public,
							};
							current.AddMember(nsd);
							current = nsd;
						}
					}
				}
	
				current.ImportReflectedType(t);
			}
		}

	//	timer.Stop();
	//	UnityEngine.Debug.Log(timer.ElapsedMilliseconds + " ms\n" + string.Join(", ", _globalNamespace.members.Keys.ToArray()));
		//	Debug.Log(_globalNamespace.Dump());

		if (builtInTypes == null)
		{
			builtInTypes = new Dictionary<string, TypeDefinitionBase>
		    {
		        { "int", DefineBuiltInType(typeof(int), "int") },
		        { "uint", DefineBuiltInType(typeof(uint), "uint") },
		        { "byte", DefineBuiltInType(typeof(byte), "byte") },
		        { "sbyte", DefineBuiltInType(typeof(sbyte), "sbyte") },
		        { "short", DefineBuiltInType(typeof(short), "short") },
		        { "ushort", DefineBuiltInType(typeof(ushort), "ushort") },
		        { "long", DefineBuiltInType(typeof(long), "long") },
		        { "ulong", DefineBuiltInType(typeof(ulong), "ulong") },
		        { "float", DefineBuiltInType(typeof(float), "float") },
		        { "double", DefineBuiltInType(typeof(float), "double") },
		        { "decimal", DefineBuiltInType(typeof(decimal), "decimal") },
		        { "char", DefineBuiltInType(typeof(char), "char") },
		        { "string", DefineBuiltInType(typeof(string), "string") },
		        { "bool", DefineBuiltInType(typeof(bool), "bool") },
		        { "object", DefineBuiltInType(typeof(object), "object") },
		        { "void", DefineBuiltInType(typeof(void), "void") },
		    };
		}

		return _globalNamespace;
	}

	public static TypeDefinitionBase DefineBuiltInType(Type type, string aliasName)
	{
		var assembly = FromAssembly(type.Assembly);
		var @namespace = assembly.FindNamespace(type.Namespace);
		var definition = @namespace.FindName(type.Name, 0, true);
		return definition as TypeDefinitionBase;
	}

	public SymbolDefinition FindNamespace(string namespaceName)
	{
		SymbolDefinition result = GlobalNamespace;
		if (string.IsNullOrEmpty(namespaceName))
			return result;
		var start = 0;
		while (start < namespaceName.Length)
		{
			var dotPos = namespaceName.IndexOf('.', start);
			var ns = dotPos == -1 ? namespaceName.Substring(start) : namespaceName.Substring(start, dotPos - start);
			result = result.FindName(ns, 0, true) as NamespaceDefinition;
			if (result == null)
				return unknownSymbol;
			start = dotPos == -1 ? int.MaxValue : dotPos + 1;
		}
		return result ?? unknownSymbol;
	}

	public void ResolveInReferencedAssemblies(ParseTree.Leaf leaf, NamespaceDefinition namespaceDefinition, int numTypeArgs)
	{
		var fullName = namespaceDefinition.FullName;
		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindNamespace(fullName);
			if (nsDef != unknownSymbol)
			{
				leaf.resolvedSymbol = nsDef.FindName(leaf.token.text, numTypeArgs, true);
				if (leaf.resolvedSymbol != null)
					return;
			}
		}
	}

	public void ResolveAttributeInReferencedAssemblies(ParseTree.Leaf leaf, NamespaceDefinition namespaceDefinition)
	{
		var fullName = namespaceDefinition.FullName;
		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindNamespace(fullName);
			if (nsDef != unknownSymbol)
			{
				leaf.resolvedSymbol = nsDef.FindName(leaf.token.text, 0, true);
				if (leaf.resolvedSymbol != null)
					return;

				leaf.resolvedSymbol = nsDef.FindName(leaf.token.text + "Attribute", 0, true);
				if (leaf.resolvedSymbol != null)
					return;
			}
		}
	}

	private static bool dontReEnter = false;

	public void GetMembersCompletionDataFromReferencedAssemblies(Dictionary<string, SymbolDefinition> data, NamespaceDefinition namespaceDefinition)
	{
		if (dontReEnter)
			return;

		var fullName = namespaceDefinition.FullName;
		foreach (var ra in referencedAssemblies)
		{ 
			var nsDef = ra.FindNamespace(fullName);	
			if (nsDef != unknownSymbol)
			{
				dontReEnter = true;
				nsDef.GetMembersCompletionData(data, 0, AccessLevelMask.Any, this);
				dontReEnter = false;
			}
		}
	}

	public void GetExtensionMethodsCompletionDataFromReferencedAssemblies(TypeDefinitionBase targetType, NamespaceDefinition namespaceDefinition, Dictionary<string, SymbolDefinition> data)
	{
//	Debug.Log("Extensions for " + targetType.GetTooltipText());
		namespaceDefinition.GetExtensionMethodsCompletionData(targetType, data, AccessLevelMask.Any);

		var fullName = namespaceDefinition.FullName;
		foreach (var ra in referencedAssemblies)
		{
			var nsDef = ra.FindNamespace(fullName) as NamespaceDefinition;	
			if (nsDef != null)
				nsDef.GetExtensionMethodsCompletionData(targetType, data, AccessLevelMask.Any);
		}
	}
}

public static class FGResolver
{
	//class ResolveReferencesVisitor : IHierarchicalVisitor<ParseTree.Node, ParseTree.Leaf>
	//{
	//    Scope currentScope;
		
	//    //public static SemanticFlags nonTopLevelScopes =
	//    //    SemanticFlags.ClassBaseScope
	//    //    | SemanticFlags.TypeParameterConstraintsScope
	//    //    | SemanticFlags.StructInterfacesScope
	//    //    | SemanticFlags.InterfaceBaseScope
	//    //    | SemanticFlags.ReturnTypeScope
	//    ////	| SemanticFlags.FormalParameterListScope
	//    //    | SemanticFlags.MethodBodyScope
	//    //    | SemanticFlags.ConstructorInitializerScope
	//    //    | SemanticFlags.LambdaExpressionBodyScope
	//    //    | SemanticFlags.AnonymousMethodBodyScope
	//    //    | SemanticFlags.CodeBlockScope
	//    //    | SemanticFlags.SwitchBlockScope
	//    //    | SemanticFlags.ForStatementScope
	//    //    | SemanticFlags.EmbeddedStatementScope
	//    //    | SemanticFlags.LocalVariableInitializerScope
	//    //    | SemanticFlags.SpecificCatchScope
	//    //    | SemanticFlags.ArgumentListScope
	//    //    | SemanticFlags.MemberInitializerScope;

	//    public ResolveReferencesVisitor(Scope resolutionContext)
	//    {
	//        currentScope = resolutionContext;

	//    //	Debug.Log("CompilationUnitScope.declaration => " + ((CompilationUnitScope)currentScope).declaration);
	//    }

	//    public bool Visit(ParseTree.Leaf parseTreeNode)
	//    {
	//        if (parseTreeNode.token.tokenKind == SyntaxToken.Kind.Identifier)
	//        {
	//            currentScope.Resolve(parseTreeNode, 0);
	//            if (parseTreeNode.resolvedSymbol == null)
	//            {
	//                if (parseTreeNode.errors == null)
	//                    parseTreeNode.errors = new List<string>();
	//                parseTreeNode.errors.Add("Unknown symbol");
	//            }
	//        }
	//        return true;
	//    }

	//    public bool VisitEnter(ParseTree.Node parseTreeNode)
	//    {
	//        if (parseTreeNode.scope != null)
	//        {
	//            //Debug.Log("Entering scope " + parseTreeNode.semantics);
	//            currentScope = parseTreeNode.scope;
	//        }
	//        //var oldDeclaration = parseTreeNode.declaration;
	//        //if (oldDeclaration != null)
	//        //{
	//        //    Debug.Log("Removing " + oldDeclaration);
	//        //    if (oldDeclaration.scope != null)
	//        //    {
	//        //        oldDeclaration.scope.RemoveDeclaration(oldDeclaration);
	//        //        oldDeclaration.scope = null;
	//        //        oldDeclaration.definition = null;
	//        //    }
	//        //}
	//        /*var rr =*/ SymbolDefinition.ResolveNode(parseTreeNode, currentScope);
	//        return false;// rr == null;
	//    }

	//    public bool VisitLeave(ParseTree.Node parseTreeNode)
	//    {
	//        if (currentScope != null && parseTreeNode.scope == currentScope)
	//        {
	//            currentScope = currentScope.parentScope;
	//            if (currentScope == null && parseTreeNode.RuleName != "compilationUnit")
	//                Debug.LogError("parentScope not set for node " + parseTreeNode.RuleName + "\n" + parseTreeNode);
	//        }
	//        return true;
	//    }
	//}


	//static NamespaceDefinition _globalNamespace;
	//private static NamespaceDefinition GlobalNamespace
	//{
	//    get { return _globalNamespace ?? InitializeGlobalNamespace(); }
	//    set { _globalNamespace = value; }
	//}
	
	//public static NamespaceDefinition InitializeGlobalNamespace()
	//{
	//    _globalNamespace = new NamespaceDefinition { name = "", kind = SymbolKind.Namespace };

	//    string projectRoot = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.Length - "Assets".Length);
	//    string assemblyPath = projectRoot + "Library/ScriptAssemblies/Assembly-CSharp-Editor.dll";
	//    //Debug.Log(mdbPath + " exists: " + System.IO.File.Exists(mdbPath));

	//    //var readerParameters = new ReaderParameters { ReadSymbols = true };
	//    //var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
	//    //foreach (var t in assemblyDefinition.MainModule.Types)
	//    //{
	//    //    if (t.HasMethods)
	//    //    {
	//    //        foreach (var method in t.Methods)
	//    //        {
	//    //            var body = method.Body;
	//    //            if (body != null && body.Scope != null)
	//    //            {
	//    //                var sp = body.Scope.Start.SequencePoint;
	//    //                Debug.Log(t.Namespace + " " + t.Name + " in " + sp.Document.Url + ":" + sp.StartLine);
	//    //                break;
	//    //            }
	//    //        }
	//    //    }
	//    //    else
	//    //        Debug.Log(t.FullName);
	//    //    //for (var i = 0; i < symFile.SourceCount; ++i)
	//    //    //{
	//    //    //    var source = symFile.Sources[i];
	//    //    //    Debug.Log(source);
	//    //    //}
	//    //}
	//    ////Mono.Cecil.Mdb.MdbReader mdbReader = new Mono.Cecil.Mdb.MdbReader(symFile);

	//    var timer = new Stopwatch();
	//    timer.Start();

	//    var executingAssembly = Assembly.GetExecutingAssembly();
	//    var referencedAssemblies = new HashSet<string>();
	//    foreach (var ra in executingAssembly.GetReferencedAssemblies())
	//        referencedAssemblies.Add(ra.FullName);

	//    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

	//    foreach (var a in assemblies)
	//    {
	//        if (a != executingAssembly && !referencedAssemblies.Contains(a.FullName))
	//            continue;

	//        var types = a == executingAssembly ? a.GetTypes() : a.GetExportedTypes();
	//        foreach (var t in types)
	//        {
	//            SymbolDefinition current = _globalNamespace;

	//            if (!string.IsNullOrEmpty(t.Namespace))
	//            {
	//                var ns = t.Namespace.Split('.');
	//                for (var i = 0; i < ns.Length; ++i)
	//                {
	//                    var nsName = ns[i];
	//                    var definition = current.FindName(nsName);
	//                    if (definition != null)
	//                    {
	//                        current = definition;
	//                    }
	//                    else
	//                    {
	//                        var nsd = new NamespaceDeclaration(nsName)
	//                        {
	//                            kind = SymbolKind.Namespace
	//                        };
	//                        current = current.AddDeclaration(nsd);
	//                    }
	//                }
	//            }

	//            if (!t.IsNested)
	//                current.ImportReflectedType(t);
	//        }
	//    }

	//    timer.Stop();
	//    Debug.Log(timer.ElapsedMilliseconds + " ms\n" + string.Join(", ", _globalNamespace.members.Keys.ToArray()));
	////	Debug.Log(_globalNamespace.Dump());

	//    if (SymbolDefinition.builtInTypes == null)
	//    {
	//        var system = typeof(int).Assembly;
	//        SymbolDefinition.builtInTypes = new Dictionary<string, TypeDefinitionBase>
	//        {
	//            { "int", DefineBuiltInType(typeof(int), "int") },
	//            { "uint", DefineBuiltInType(typeof(uint), "uint") },
	//            { "byte", DefineBuiltInType(typeof(byte), "byte") },
	//            { "sbyte", DefineBuiltInType(typeof(sbyte), "sbyte") },
	//            { "short", DefineBuiltInType(typeof(short), "short") },
	//            { "ushort", DefineBuiltInType(typeof(ushort), "ushort") },
	//            { "long", DefineBuiltInType(typeof(long), "long") },
	//            { "ulong", DefineBuiltInType(typeof(ulong), "ulong") },
	//            { "float", DefineBuiltInType(typeof(float), "float") },
	//            { "double", DefineBuiltInType(typeof(float), "double") },
	//            { "decimal", DefineBuiltInType(typeof(decimal), "decimal") },
	//            { "char", DefineBuiltInType(typeof(char), "char") },
	//            { "string", DefineBuiltInType(typeof(string), "string") },
	//            { "bool", DefineBuiltInType(typeof(bool), "bool") },
	//            { "object", DefineBuiltInType(typeof(object), "object") },
	//            { "void", DefineBuiltInType(typeof(void), "void") },
	//        };
	//    }
	//    //foreach (var s in new[] { "int", "float", "bool", "string", "object" })
	//    //    SymbolDefinition.builtInTypes;

	//    return _globalNamespace;
	//}

	//public static TypeDefinitionBase DefineBuiltInType(Type type, string aliasName)
	//{
	//    var @namespace = FindNamespace(type.Namespace);
	//    var definition = @namespace.FindName(type.Name);
	//    return definition as TypeDefinitionBase;
	//}

	//public static void ScanSymbols(ParseTree.BaseNode parseTreeNode, string bufferName)
	//{
	//    var timer = new Stopwatch();
	//    timer.Start();

	//    var sdVisitor = new ScanDeclarationsVisitor(bufferName, parseTreeNode);
	//    parseTreeNode.Accept(sdVisitor);

	//    //var rdVisitor = new ResolveDeclarationsVisitor(GlobalNamespace);
	//    //sdVisitor.symbolTable.Accept(rdVisitor);

	//    timer.Stop();
	//    Debug.Log("ScanSymbols(" + bufferName + ") took " + timer.ElapsedMilliseconds + " ms");
	//}

	/*
	public static void ResolveSymbols(ParseTree.BaseNode parseTreeNode, string bufferName)
	{
		return;
		var unitScope = ((ParseTree.Node) parseTreeNode).scope as CompilationUnitScope;
		if (unitScope == null)
			return;

	//	var timer = new Stopwatch();
	//	timer.Start();

		if (unitScope.assembly == null)
		{
			unitScope.assembly = AssemblyDefinition.FromAssetPath(bufferName);
		}
		var rrVisitor = new ResolveReferencesVisitor(unitScope);
	//	var constructing = timer.ElapsedMilliseconds;
		parseTreeNode.Accept(rrVisitor);

	//	timer.Stop();
	//	UnityEngine.Debug.Log("ResolveSymbols(" + bufferName + ") took " + timer.ElapsedMilliseconds + " ms (c: " + constructing + " ms).");
	//	Debug.Log(GlobalNamespace.Dump());
	}
	*/

	//public static SymbolDefinition FindNamespace(string namespaceName)
	//{
	//    SymbolDefinition result = GlobalNamespace;
	//    if (string.IsNullOrEmpty(namespaceName))
	//        return result;
	//    var namespaces = namespaceName.Split('.');
	//    foreach (var ns in namespaces)
	//    {
	//        if (result == null)
	//            return SymbolDefinition.unknownSymbol;
	//        result = result.FindName(ns);
	//    }
	//    return result;
	//}

	public static void GetCompletions(IdentifierCompletionsType completionTypes, ParseTree.BaseNode parseTreeNode, HashSet<SymbolDefinition> completionSymbols, string assetPath)
	{
		try
		{
			var d = new Dictionary<string, SymbolDefinition>();
			var assemblyDefinition = AssemblyDefinition.FromAssetPath(assetPath);
			
			if ((completionTypes & IdentifierCompletionsType.Member) != 0)
			{
			//	completionTypes &= ~IdentifierCompletionsType.Namespace;
	
				var target = parseTreeNode.FindPreviousNode();
			//	UnityEngine.Debug.Log(target);
				if (target != null)
				{
					var targetAsNode = target as ParseTree.Node;
					ResolveNode(targetAsNode ?? target.parent);
					var targetDef = SymbolDefinition.ResolveNode(targetAsNode ?? target.parent);
					if (targetDef != null)
					{
						var typeOf = targetDef.TypeOf();
					//	UnityEngine.Debug.Log(typeOf);
	
						var flags = BindingFlags.Instance | BindingFlags.Static;
						switch (targetDef.kind)
						{
							case SymbolKind.None:
							case SymbolKind.Error:
								break;
							case SymbolKind.Namespace:
							case SymbolKind.Interface:
							case SymbolKind.Struct:
							case SymbolKind.Class:
							case SymbolKind.TypeParameter:
							case SymbolKind.Delegate:
								flags = BindingFlags.Static;
								break;
							case SymbolKind.Enum:
								flags = BindingFlags.Static;
								break;
							case SymbolKind.Field:
							case SymbolKind.ConstantField:
							case SymbolKind.LocalConstant:
							case SymbolKind.Property:
							case SymbolKind.Event:
							case SymbolKind.Indexer:
							case SymbolKind.Method:
							case SymbolKind.MethodGroup:
							case SymbolKind.Constructor:
							case SymbolKind.Destructor:
							case SymbolKind.Operator:
							case SymbolKind.Accessor:
							case SymbolKind.Parameter:
							case SymbolKind.CatchParameter:
							case SymbolKind.Variable:
							case SymbolKind.ForEachVariable:
							case SymbolKind.FromClauseVariable:
							case SymbolKind.EnumMember:
								flags = BindingFlags.Instance;
								break;
							case SymbolKind.BaseTypesList:
								flags = BindingFlags.Static;
								break;
							case SymbolKind.Instance:
								flags = BindingFlags.Instance;
								break;
							case SymbolKind.Null:
								return;
							default:
								throw new ArgumentOutOfRangeException();
						}
						//targetDef.kind = targetDef is TypeDefinitionBase && targetDef.kind != SymbolKind.Enum ? BindingFlags.Static : targetDef is InstanceDefinition ? BindingFlags.Instance : 0;
	
						TypeDefinitionBase contextType = null;
						for (var n = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent; n != null; n = n.parent)
						{
							var s = n.scope as SymbolDeclarationScope;
							if (s != null)
							{
								contextType = s.declaration.definition as TypeDefinitionBase;
								if (contextType != null)
									break;
							}
						}
	
						AccessLevelMask mask =
							typeOf == contextType ? AccessLevelMask.Private | AccessLevelMask.Protected | AccessLevelMask.Internal | AccessLevelMask.Public :
							typeOf.IsSameOrParentOf(contextType) ? AccessLevelMask.Protected | AccessLevelMask.Internal | AccessLevelMask.Public :
							AccessLevelMask.Internal | AccessLevelMask.Public;
	
	//					var enclosingScopeNode = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent;
	//					while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
	//						enclosingScopeNode = enclosingScopeNode.parent;
	//					var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;
	
						//UnityEngine.Debug.Log(flags + "\n" + mask);
						/*targetDef*/typeOf.GetMembersCompletionData(d, flags, mask, assemblyDefinition);
	
						//if (flags == BindingFlags.Instance &&
						//	(typeOf.kind == SymbolKind.Class || typeOf.kind == SymbolKind.Struct || typeOf.kind == SymbolKind.Interface || typeOf.kind == SymbolKind.Enum))
						//{
						//	var enclosingScopeNode = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent;
						//	while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
						//		enclosingScopeNode = enclosingScopeNode.parent;
						//	var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;
	
						//	var visibleNamespaces = new HashSet<NamespaceDefinition>(enclosingScope.VisibleNamespacesInScope());
						//	foreach (var ns in visibleNamespaces)
						//		assemblyDefinition.GetExtensionMethodsCompletionDataFromReferencedAssemblies(typeOf as TypeDefinitionBase, ns, d);
						//}
					}
				}
			}
			else if (parseTreeNode == null)
			{
				UnityEngine.Debug.LogWarning(completionTypes);
			}
			else
			{
				if (parseTreeNode.IsLit("=>"))
				{
					parseTreeNode = parseTreeNode.parent.NodeAt(parseTreeNode.childIndex + 1);
				}

				var enclosingScopeNode = parseTreeNode as ParseTree.Node ?? parseTreeNode.parent;
				while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
					enclosingScopeNode = enclosingScopeNode.parent;
				if (enclosingScopeNode != null)
				{
					var lastLeaf = parseTreeNode as ParseTree.Leaf ??
						((ParseTree.Node) parseTreeNode).GetLastLeaf() ??
						((ParseTree.Node) parseTreeNode).FindPreviousLeaf();
					Scope.completionAtLine = lastLeaf != null ? lastLeaf.line : 0;
					Scope.completionAtTokenIndex = lastLeaf != null ? lastLeaf.tokenIndex : 0;
					
				//	UnityEngine.Debug.Log(enclosingScopeNode.scope);
					enclosingScopeNode.scope.GetCompletionData(d, true, assemblyDefinition);
				}
			}
	
			completionSymbols.UnionWith(d.Values);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public static ParseTree.Node ResolveNode(ParseTree.Node node)
	{
		if (node == null)
			return null;
		
	//	UnityEngine.Debug.Log(node.RuleName);
		while (node.parent != null)
		{
			switch (node.RuleName)
			{
				case "primaryExpression":
				case "primaryExpressionStart":
				case "primaryExpressionPart":
				case "objectCreationExpression":
				case "objectOrCollectionInitializer":
				case "typeOrGeneric":
				case "namespaceOrTypeName":
				case "typeName":
				case "nonArrayType":
				//case "attribute":
				case "accessIdentifier":
				case "brackets":
				case "argumentList":
				case "argumentName":
				case "argument":
//				case "VAR":
//				case "localVariableType":
//				case "localVariableDeclaration":
				case "arrayCreationExpression":
				case "implicitArrayCreationExpression":
				case "arrayInitializer":
				case "arrayInitializerList":
//				case "qid":
				case "qidStart":
				case "qidPart":
				case "memberInitializer":
//				case "memberName":
			//	case "unaryExpression":
			//	case "modifiers":
					node = node.parent;
				//	UnityEngine.Debug.Log("--> " + node.RuleName);
					continue;
			}
			break;
		}
		
		try
		{
			//var numTypeArgs = 0;
			//var parent = node.parent;
			//if (parent != null)
			//{
			//	var nextNode = node.NodeAt(node.childIndex + 1);
			//	if (nextNode != null)
			//	{
			//		if (nextNode.RuleName == "typeArgumentList")
			//			numTypeArgs = (nextNode.numValidNodes + 1) / 2;
			//		else if (nextNode.RuleName == "typeParameterList")
			//			numTypeArgs = (nextNode.numValidNodes + 2) / 3;
			//		else if (nextNode.RuleName == "unboundTypeRank")
			//			numTypeArgs = nextNode.numValidNodes - 1;
			//	}
			//}
			var result = SymbolDefinition.ResolveNode(node, null, null, 0);//numTypeArgs);
			if (result == null)
				ResolveChildren(node);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			return null;
		}
		
		return node;
	}

	static void ResolveChildren(ParseTree.Node node)
	{
		if (node == null)
			return;
		if (node.numValidNodes != 0)
		{
			for (var i = 0; i < node.numValidNodes; ++i)
			{
				var child = node.ChildAt(i);
				
				var leaf = child as ParseTree.Leaf;
				if (leaf == null ||
				    leaf.token != null &&
				    leaf.token.tokenKind != SyntaxToken.Kind.Punctuator &&
					(leaf.token.tokenKind != SyntaxToken.Kind.Keyword || SymbolDefinition.builtInTypes.ContainsKey(leaf.token.text)))
				{
					if (leaf == null)
					{
						switch (((ParseTree.Node) child).RuleName)
						{
							case "modifiers":
							case "methodBody":
								continue;
						}
					}
					var numTypeArgs = 0;
					//var nextNode = node.NodeAt(i + 1);
					//if (nextNode != null)
					//{
					//	if (nextNode.RuleName == "typeArgumentList")
					//		numTypeArgs = (nextNode.numValidNodes + 1) / 2;
					//	else if (nextNode.RuleName == "typeParameterList")
					//		numTypeArgs = (nextNode.numValidNodes + 2) / 3;
					//	else if (nextNode.RuleName == "unboundTypeRank")
					//		numTypeArgs = nextNode.numValidNodes - 1;
					//}
					if (SymbolDefinition.ResolveNode(child, null, null, numTypeArgs) == null)
					{
						var childAsNode = child as ParseTree.Node;
						if (childAsNode != null)
							ResolveChildren(childAsNode);
					}
				}
			}
		}
	}
}

}
