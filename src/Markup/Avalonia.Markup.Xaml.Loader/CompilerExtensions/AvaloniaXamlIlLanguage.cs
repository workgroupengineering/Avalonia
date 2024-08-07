using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    /*
        This file is used in the build task.
        ONLY use types from netstandard and XamlIl. NO dependencies on Avalonia are allowed. Only strings.
        No, nameof isn't welcome here either
     */
    
    class AvaloniaXamlIlLanguage
    {
        public static (XamlLanguageTypeMappings language, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emit) Configure(IXamlTypeSystem typeSystem)
        {
            var runtimeHelpers = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
            var rv = new XamlLanguageTypeMappings(typeSystem)
            {
                SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize"),
                XmlnsAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute"),
                },
                ContentAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.ContentAttribute")
                },
                WhitespaceSignificantCollectionAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.WhitespaceSignificantCollectionAttribute")
                },
                TrimSurroundingWhitespaceAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.TrimSurroundingWhitespaceAttribute")
                },
                ProvideValueTarget = typeSystem.GetType("Avalonia.Markup.Xaml.IProvideValueTarget"),
                RootObjectProvider = typeSystem.GetType("Avalonia.Markup.Xaml.IRootObjectProvider"),
                RootObjectProviderIntermediateRootPropertyName = "IntermediateRootObject",
                UriContextProvider = typeSystem.GetType("Avalonia.Markup.Xaml.IUriContext"),
                ParentStackProvider =
                    typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlParentStackProvider"),

                XmlNamespaceInfoProvider =
                    typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlXmlNamespaceInfoProvider"),
                DeferredContentPropertyAttributes = { typeSystem.GetType("Avalonia.Metadata.TemplateContentAttribute") },
                DeferredContentExecutorCustomizationDefaultTypeParameter = typeSystem.GetType("Avalonia.Controls.Control"),
                DeferredContentExecutorCustomizationTypeParameterDeferredContentAttributePropertyNames = new List<string>
                {
                    "TemplateResultType"
                },
                DeferredContentExecutorCustomization =
                    runtimeHelpers.FindMethod(m => m.Name == "DeferredTransformationFactoryV3"),
                UsableDuringInitializationAttributes =
                {
                    typeSystem.GetType("Avalonia.Metadata.UsableDuringInitializationAttribute"),
                },
                InnerServiceProviderFactoryMethod =
                    runtimeHelpers.FindMethod(m => m.Name == "CreateInnerServiceProviderV1"),
                IAddChild = typeSystem.GetType("Avalonia.Metadata.IAddChild"),
                IAddChildOfT = typeSystem.GetType("Avalonia.Metadata.IAddChild`1")
            };
            rv.CustomAttributeResolver = new AttributeResolver(typeSystem, rv);

            var emit = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>
            {
                ProvideValueTargetPropertyEmitter = XamlIlAvaloniaPropertyHelper.EmitProvideValueTarget,
                ContextTypeBuilderCallback = definition =>
                {
                    EmitNameScopeField(rv, typeSystem, definition);
                    EmitEagerParentStackProvider(rv, typeSystem, definition, runtimeHelpers);
                }
            };
            return (rv, emit);
        }

        public const string ContextNameScopeFieldName = "AvaloniaNameScope";

        private static void EmitNameScopeField(
            XamlLanguageTypeMappings mappings,
            IXamlTypeSystem typeSystem,
            IXamlILContextDefinition<IXamlILEmitter> definition)
        {
            var nameScopeType = typeSystem.GetType("Avalonia.Controls.INameScope");
            var field = definition.TypeBuilder.DefineField(nameScopeType,
                ContextNameScopeFieldName, XamlVisibility.Public, false);
            definition.ConstructorBuilder.Generator
                .Ldarg_0()
                .Ldarg(1)
                .Ldtype(nameScopeType)
                .EmitCall(mappings.ServiceProvider.GetMethod(new FindMethodMethodSignature("GetService",
                    typeSystem.GetType("System.Object"), typeSystem.GetType("System.Type"))))
                .Stfld(field);
        }

        private static void EmitEagerParentStackProvider(
            XamlLanguageTypeMappings mappings,
            IXamlTypeSystem typeSystem,
            IXamlILContextDefinition<IXamlILEmitter> definition,
            IXamlType runtimeHelpers)
        {
            var interfaceType = typeSystem.GetType("Avalonia.Markup.Xaml.XamlIl.Runtime.IAvaloniaXamlIlEagerParentStackProvider");

            definition.TypeBuilder.AddInterfaceImplementation(interfaceType);

            // IReadOnlyList<object> DirectParentsStack => (IReadOnlyList<object>)ParentsStack;
            var directParentsGetter = ImplementInterfacePropertyGetter("DirectParentsStack");
            directParentsGetter.Generator
                .LdThisFld(definition.ParentListField!)
                .Castclass(directParentsGetter.ReturnType)
                .Ret();

            var serviceProviderGetServiceMethod = mappings.ServiceProvider.GetMethod(new FindMethodMethodSignature(
                "GetService",
                typeSystem.GetType("System.Object"),
                typeSystem.GetType("System.Type")));

            var asEagerParentStackProviderMethod = runtimeHelpers.GetMethod(new FindMethodMethodSignature(
                "AsEagerParentStackProvider",
                interfaceType,
                mappings.ParentStackProvider!)
            {
                IsStatic = true
            });

            // IAvaloniaXamlIlEagerParentStackProvider? ParentProvider
            // => XamlIlRuntimeHelpers.AsEagerParentStackProvider(_serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider)));
            var parentProviderGetter = ImplementInterfacePropertyGetter("ParentProvider");
            parentProviderGetter.Generator
                .LdThisFld(definition.ParentServiceProviderField)
                .Ldtype(mappings.ParentStackProvider!)
                .EmitCall(serviceProviderGetServiceMethod)
                .EmitCall(asEagerParentStackProviderMethod)
                .Ret();

            IXamlMethodBuilder<IXamlILEmitter> ImplementInterfacePropertyGetter(string propertyName)
            {
                var interfaceGetter = interfaceType.GetMethod(m => m.Name == "get_" + propertyName);

                var getter = definition.TypeBuilder.DefineMethod(
                    interfaceGetter.ReturnType,
                    Array.Empty<IXamlType>(),
                    "get_" + propertyName,
                    XamlVisibility.Private,
                    false,
                    true,
                    interfaceGetter);

                definition.TypeBuilder.DefineProperty(interfaceGetter.ReturnType, propertyName, null, getter);

                return getter;
            }
        }

        class AttributeResolver : IXamlCustomAttributeResolver
        {
            private readonly IXamlType _typeConverterAttribute;

            private readonly List<KeyValuePair<IXamlType, IXamlType>> _converters =
                new List<KeyValuePair<IXamlType, IXamlType>>();

            private readonly IXamlType _avaloniaList;
            private readonly IXamlType _avaloniaListConverter;


            public AttributeResolver(IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings)
            {
                _typeConverterAttribute = mappings.TypeConverterAttributes.First();

                void AddType(IXamlType type, IXamlType conv) 
                    => _converters.Add(new KeyValuePair<IXamlType, IXamlType>(type, conv));
                
                AddType(typeSystem.GetType("Avalonia.Media.IImage"), typeSystem.GetType("Avalonia.Markup.Xaml.Converters.BitmapTypeConverter"));
                AddType(typeSystem.GetType("Avalonia.Media.Imaging.Bitmap"), typeSystem.GetType("Avalonia.Markup.Xaml.Converters.BitmapTypeConverter"));
                AddType(typeSystem.GetType("Avalonia.Media.IImageBrushSource"), typeSystem.GetType("Avalonia.Markup.Xaml.Converters.BitmapTypeConverter"));
                var ilist = typeSystem.GetType("System.Collections.Generic.IList`1");
                AddType(ilist.MakeGenericType(typeSystem.GetType("Avalonia.Point")),
                    typeSystem.GetType("Avalonia.Markup.Xaml.Converters.PointsListTypeConverter"));
                AddType(typeSystem.GetType("Avalonia.Controls.WindowIcon"), typeSystem.GetType("Avalonia.Markup.Xaml.Converters.IconTypeConverter"));
                AddType(typeSystem.GetType("System.Globalization.CultureInfo"), typeSystem.GetType( "System.ComponentModel.CultureInfoConverter"));
                AddType(typeSystem.GetType("System.Uri"), typeSystem.GetType( "Avalonia.Markup.Xaml.Converters.AvaloniaUriTypeConverter"));
                AddType(typeSystem.GetType("System.TimeSpan"), typeSystem.GetType( "Avalonia.Markup.Xaml.Converters.TimeSpanTypeConverter"));
                AddType(typeSystem.GetType("Avalonia.Media.FontFamily"), typeSystem.GetType("Avalonia.Markup.Xaml.Converters.FontFamilyTypeConverter"));
                _avaloniaList = typeSystem.GetType("Avalonia.Collections.AvaloniaList`1");
                _avaloniaListConverter = typeSystem.GetType("Avalonia.Collections.AvaloniaListConverter`1");
            }

            IXamlType? LookupConverter(IXamlType type)
            {
                foreach(var p in _converters)
                    if (p.Key.Equals(type))
                        return p.Value;
                if (type.GenericTypeDefinition?.Equals(_avaloniaList) == true)
                    return _avaloniaListConverter.MakeGenericType(type.GenericArguments[0]);
                return null;
            }

            class ConstructedAttribute : IXamlCustomAttribute
            {
                public bool Equals(IXamlCustomAttribute? other) => false;
                
                public IXamlType Type { get; }
                public List<object?> Parameters { get; }
                public Dictionary<string, object?> Properties { get; }

                public ConstructedAttribute(IXamlType type, List<object?>? parameters, Dictionary<string, object?>? properties)
                {
                    Type = type;
                    Parameters = parameters ?? new List<object?>();
                    Properties = properties ?? new Dictionary<string, object?>();
                }
            }
            
            public IXamlCustomAttribute? GetCustomAttribute(IXamlType type, IXamlType attributeType)
            {
                if (attributeType.Equals(_typeConverterAttribute))
                {
                    var conv = LookupConverter(type);
                    if (conv != null)
                        return new ConstructedAttribute(_typeConverterAttribute, [conv], null);
                }

                return null;
            }

            public IXamlCustomAttribute? GetCustomAttribute(IXamlProperty property, IXamlType attributeType)
            {
                return null;
            }
        }

        public static bool CustomValueConverter(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IReadOnlyList<IXamlCustomAttribute>? customAttributes,
            IXamlType type,
            [NotNullWhen(true)] out IXamlAstValueNode? result)
        {
            if (node is AvaloniaXamlIlOptionMarkupExtensionTransformer.OptionsMarkupExtensionNode optionsNode)
            {
                if (optionsNode.ConvertToReturnType(context, type, out var newOptionsNode))
                {
                    result = newOptionsNode;
                    return true;
                }
            }

            if (!(node is XamlAstTextNode textNode))
            {
                result = null;
                return false;
            }

            var text = textNode.Text;
            var types = context.GetAvaloniaTypes();

            if (AvaloniaXamlIlLanguageParseIntrinsics.TryConvert(context, node, text, type, types, out result))
            {
                return true;
            }

            if (type.FullName == "Avalonia.AvaloniaProperty")
            {
                var attrType = context.GetAvaloniaTypes().InheritDataTypeFromAttribute;
                var scopeKind = customAttributes?
                        .FirstOrDefault(a => a.Type.Equals(attrType))?.Parameters
                        .FirstOrDefault() switch
                    {
                        1 => AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style,
                        2 => AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate,
                        _ => (AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes?)null
                    };

                var scope = context.ParentNodes().OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault(s => scopeKind.HasValue ? s.ScopeType == scopeKind : true);
                if (scope == null)
                {
#if NET6_0_OR_GREATER
                    var isScopeDefined = Enum.IsDefined<AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes>(scopeKind ?? default);
#else
                    var isScopeDefined = Enum.IsDefined(typeof(AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes), scopeKind ?? default);
#endif
                    var scopeKindStr = isScopeDefined ? scopeKind!.Value.ToString() : "parent"; 
                    throw new XamlX.XamlLoadException($"Unable to find the {scopeKindStr} scope for AvaloniaProperty lookup", node);
                }

                result = XamlIlAvaloniaPropertyHelper.CreateNode(context, text, scope.TargetType, node );
                return true;
            }

            result = null;
            return false;
        }
    }
}
