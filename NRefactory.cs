using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem
{
    using ICSharpCode.NRefactory.TypeSystem.Implementation;
    using ICSharpCode.NRefactory.Utils;
    /// <summary>
    /// Represents the result of resolving an expression.
    /// </summary>
    public class ResolveResult
    {
        readonly IType type;

        public ResolveResult(IType type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            this.type = type;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
                                                         Justification = "Unrelated to object.GetType()")]
        public IType Type
        {
            get { return type; }
        }

        public virtual bool IsCompileTimeConstant
        {
            get { return false; }
        }

        public virtual object ConstantValue
        {
            get { return null; }
        }

        public virtual bool IsError
        {
            get { return false; }
        }

        public override string ToString()
        {
            return "[" + GetType().Name + " " + type + "]";
        }

        public virtual IEnumerable<ResolveResult> GetChildResults()
        {
            return Enumerable.Empty<ResolveResult>();
        }

        public virtual DomRegion GetDefinitionRegion()
        {
            return DomRegion.Empty;
        }

        public virtual ResolveResult ShallowClone()
        {
            return (ResolveResult)MemberwiseClone();
        }
    }
    /// <summary>
    /// Represents an unresolved constant value.
    /// </summary>
    public interface IConstantValue
    {
        /// <summary>
        /// Resolves the value of this constant.
        /// </summary>
        /// <param name="context">Context where the constant value will be used.</param>
        /// <returns>Resolve result representing the constant value.
        /// This method never returns null; in case of errors, an ErrorResolveResult will be returned.</returns>
        ResolveResult Resolve(ITypeResolveContext context);
    }
    /// <summary>
    /// Type parameter of a generic class/method.
    /// </summary>
    public interface IUnresolvedTypeParameter : INamedElement
    {
        /// <summary>
        /// Get the type of this type parameter's owner.
        /// </summary>
        /// <returns>SymbolKind.TypeDefinition or SymbolKind.Method</returns>
        SymbolKind OwnerType { get; }

        /// <summary>
        /// Gets the index of the type parameter in the type parameter list of the owning method/class.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the list of attributes declared on this type parameter.
        /// </summary>
        IList<IUnresolvedAttribute> Attributes { get; }

        /// <summary>
        /// Gets the variance of this type parameter.
        /// </summary>
        VarianceModifier Variance { get; }

        /// <summary>
        /// Gets the region where the type parameter is defined.
        /// </summary>
        DomRegion Region { get; }

        ITypeParameter CreateResolvedTypeParameter(ITypeResolveContext context);
    }
    /// <summary>
    /// Represents a variable (name/type pair).
    /// </summary>
    public interface IVariable : ISymbol
    {
        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        new string Name { get; }

        /// <summary>
        /// Gets the declaration region of the variable.
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Gets the type of the variable.
        /// </summary>
        IType Type { get; }

        /// <summary>
        /// Gets whether this variable is a constant (C#-like const).
        /// </summary>
        bool IsConst { get; }

        /// <summary>
        /// If this field is a constant, retrieves the value.
        /// For parameters, this is the default value.
        /// </summary>
        object ConstantValue { get; }
    }
    public interface IParameter : IVariable
    {
        /// <summary>
        /// Gets the list of attributes.
        /// </summary>
        IList<IAttribute> Attributes { get; }

        /// <summary>
        /// Gets whether this parameter is a C# 'ref' parameter.
        /// </summary>
        bool IsRef { get; }

        /// <summary>
        /// Gets whether this parameter is a C# 'out' parameter.
        /// </summary>
        bool IsOut { get; }

        /// <summary>
        /// Gets whether this parameter is a C# 'params' parameter.
        /// </summary>
        bool IsParams { get; }

        /// <summary>
        /// Gets whether this parameter is optional.
        /// The default value is given by the <see cref="IVariable.ConstantValue"/> property.
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// Gets the owner of this parameter.
        /// May return null; for example when parameters belong to lambdas or anonymous methods.
        /// </summary>
        IParameterizedMember Owner { get; }
    }
    /// <summary>
    /// Represents an unresolved attribute.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IUnresolvedAttribute
    {
        /// <summary>
        /// Gets the code region of this attribute.
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Resolves the attribute.
        /// </summary>
        IAttribute CreateResolvedAttribute(ITypeResolveContext context);
    }
    public interface IUnresolvedParameter
    {
        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the declaration region of the variable.
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Gets the type of the variable.
        /// </summary>
        ITypeReference Type { get; }

        /// <summary>
        /// Gets the list of attributes.
        /// </summary>
        IList<IUnresolvedAttribute> Attributes { get; }

        /// <summary>
        /// Gets whether this parameter is a C# 'ref' parameter.
        /// </summary>
        bool IsRef { get; }

        /// <summary>
        /// Gets whether this parameter is a C# 'out' parameter.
        /// </summary>
        bool IsOut { get; }

        /// <summary>
        /// Gets whether this parameter is a C# 'params' parameter.
        /// </summary>
        bool IsParams { get; }

        /// <summary>
        /// Gets whether this parameter is optional.
        /// </summary>
        bool IsOptional { get; }

        IParameter CreateResolvedParameter(ITypeResolveContext context);
    }
    /// <summary>
    /// Represents a method or property.
    /// </summary>
    public interface IUnresolvedParameterizedMember : IUnresolvedMember
    {
        IList<IUnresolvedParameter> Parameters { get; }
    }
    public interface IUnresolvedMethod : IUnresolvedParameterizedMember
    {
        /// <summary>
        /// Gets the attributes associated with the return type. (e.g. [return: MarshalAs(...)])
        /// </summary>
        IList<IUnresolvedAttribute> ReturnTypeAttributes { get; }

        IList<IUnresolvedTypeParameter> TypeParameters { get; }

        bool IsConstructor { get; }
        bool IsDestructor { get; }
        bool IsOperator { get; }

        /// <summary>
        /// Gets whether the method is a C#-style partial method.
        /// Check <see cref="HasBody"/> to test if it is a partial method declaration or implementation.
        /// </summary>
        bool IsPartial { get; }

        /// <summary>
        /// Gets whether the method is a C#-style async method.
        /// </summary>
        bool IsAsync { get; }

        bool IsExtensionMethod { get; }

        [Obsolete("Use IsPartial && !HasBody instead")]
        bool IsPartialMethodDeclaration { get; }

        [Obsolete("Use IsPartial && HasBody instead")]
        bool IsPartialMethodImplementation { get; }

        /// <summary>
        /// Gets whether the method has a body.
        /// This property returns <c>false</c> for <c>abstract</c> or <c>extern</c> methods,
        /// or for <c>partial</c> methods without implementation.
        /// </summary>
        bool HasBody { get; }

        /// <summary>
        /// If this method is an accessor, returns a reference to the corresponding property/event.
        /// Otherwise, returns null.
        /// </summary>
        IUnresolvedMember AccessorOwner { get; }

        /// <summary>
        /// Resolves the member.
        /// </summary>
        /// <param name="context">
        /// Context for looking up the member. The context must specify the current assembly.
        /// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
        /// </param>
        /// <returns>
        /// Returns the resolved member, or <c>null</c> if the member could not be found.
        /// </returns>
        new IMethod Resolve(ITypeResolveContext context);
    }
    [Flags]
    public enum GetMemberOptions
    {
        /// <summary>
        /// No options specified - this is the default.
        /// Members will be specialized, and inherited members will be included.
        /// </summary>
        None = 0x00,
        /// <summary>
        /// Do not specialize the returned members - directly return the definitions.
        /// </summary>
        ReturnMemberDefinitions = 0x01,
        /// <summary>
        /// Do not list inherited members - only list members defined directly on this type.
        /// </summary>
        IgnoreInheritedMembers = 0x02
    }

    /// <summary>
    /// Represents a property or indexer.
    /// </summary>
    public interface IProperty : IParameterizedMember
    {
        bool CanGet { get; }
        bool CanSet { get; }

        IMethod Getter { get; }
        IMethod Setter { get; }

        bool IsIndexer { get; }
    }

    /// <summary>
    /// Represents a property or indexer.
    /// </summary>
    public interface IUnresolvedProperty : IUnresolvedParameterizedMember
    {
        bool CanGet { get; }
        bool CanSet { get; }

        IUnresolvedMethod Getter { get; }
        IUnresolvedMethod Setter { get; }

        bool IsIndexer { get; }

        /// <summary>
        /// Resolves the member.
        /// </summary>
        /// <param name="context">
        /// Context for looking up the member. The context must specify the current assembly.
        /// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
        /// </param>
        /// <returns>
        /// Returns the resolved member, or <c>null</c> if the member could not be found.
        /// </returns>
        new IProperty Resolve(ITypeResolveContext context);
    }

    /// <summary>
    /// Represents a field or constant.
    /// </summary>
    public interface IUnresolvedField : IUnresolvedMember
    {
        /// <summary>
        /// Gets whether this field is readonly.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets whether this field is volatile.
        /// </summary>
        bool IsVolatile { get; }

        /// <summary>
        /// Gets whether this field is a constant (C#-like const).
        /// </summary>
        bool IsConst { get; }

        /// <summary>
        /// Gets whether this field is a fixed size buffer (C#-like fixed).
        /// If this is true, then ConstantValue contains the size of the buffer.
        /// </summary>
        bool IsFixed { get; }


        IConstantValue ConstantValue { get; }

        /// <summary>
        /// Resolves the member.
        /// </summary>
        /// <param name="context">
        /// Context for looking up the member. The context must specify the current assembly.
        /// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
        /// </param>
        /// <returns>
        /// Returns the resolved member, or <c>null</c> if the member could not be found.
        /// </returns>
        new IField Resolve(ITypeResolveContext context);
    }

    /// <summary>
    /// Represents a field or constant.
    /// </summary>
    public interface IField : IMember, IVariable
    {
        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        new string Name { get; } // solve ambiguity between IMember.Name and IVariable.Name

        /// <summary>
        /// Gets the region where the field is declared.
        /// </summary>
        new DomRegion Region { get; } // solve ambiguity between IEntity.Region and IVariable.Region

        /// <summary>
        /// Gets whether this field is readonly.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets whether this field is volatile.
        /// </summary>
        bool IsVolatile { get; }

        /// <summary>
        /// Gets whether this field is a fixed size buffer (C#-like fixed).
        /// If this is true, then ConstantValue contains the size of the buffer.
        /// </summary>
        bool IsFixed { get; }

        new IMemberReference ToReference(); // solve ambiguity between IMember.ToReference() and IVariable.ToReference()
    }

    public interface IUnresolvedEvent : IUnresolvedMember
    {
        bool CanAdd { get; }
        bool CanRemove { get; }
        bool CanInvoke { get; }

        IUnresolvedMethod AddAccessor { get; }
        IUnresolvedMethod RemoveAccessor { get; }
        IUnresolvedMethod InvokeAccessor { get; }

        /// <summary>
        /// Resolves the member.
        /// </summary>
        /// <param name="context">
        /// Context for looking up the member. The context must specify the current assembly.
        /// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
        /// </param>
        /// <returns>
        /// Returns the resolved member, or <c>null</c> if the member could not be found.
        /// </returns>
        new IEvent Resolve(ITypeResolveContext context);
    }

    public interface IEvent : IMember
    {
        bool CanAdd { get; }
        bool CanRemove { get; }
        bool CanInvoke { get; }

        IMethod AddAccessor { get; }
        IMethod RemoveAccessor { get; }
        IMethod InvokeAccessor { get; }
    }

    /// <summary>
    /// Default implementation for IType interface.
    /// </summary>
    [Serializable]
    public abstract class AbstractType : IType
    {
        public virtual string FullName
        {
            get
            {
                string ns = this.Namespace;
                string name = this.Name;
                if (string.IsNullOrEmpty(ns))
                {
                    return name;
                }
                else
                {
                    return ns + "." + name;
                }
            }
        }

        public abstract string Name { get; }

        public virtual string Namespace
        {
            get { return string.Empty; }
        }

        public virtual string ReflectionName
        {
            get { return this.FullName; }
        }

        public abstract bool? IsReferenceType { get; }

        public abstract TypeKind Kind { get; }

        public virtual int TypeParameterCount
        {
            get { return 0; }
        }

        readonly static IList<IType> emptyTypeArguments = new IType[0];
        public virtual IList<IType> TypeArguments
        {
            get { return emptyTypeArguments; }
        }

        public virtual IType DeclaringType
        {
            get { return null; }
        }

        public virtual bool IsParameterized
        {
            get { return false; }
        }

        public virtual ITypeDefinition GetDefinition()
        {
            return null;
        }

        public virtual IEnumerable<IType> DirectBaseTypes
        {
            get { return EmptyList<IType>.Instance; }
        }

        public abstract ITypeReference ToTypeReference();

        public virtual IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IType>.Instance;
        }

        public virtual IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IType>.Instance;
        }

        public virtual IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IMethod>.Instance;
        }

        public virtual IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IMethod>.Instance;
        }

        public virtual IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
        {
            return EmptyList<IMethod>.Instance;
        }

        public virtual IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IProperty>.Instance;
        }

        public virtual IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IField>.Instance;
        }

        public virtual IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IEvent>.Instance;
        }

        public virtual IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            IEnumerable<IMember> members = GetMethods(filter, options);
            return members
                .Concat(GetProperties(filter, options))
                .Concat(GetFields(filter, options))
                .Concat(GetEvents(filter, options));
        }

        public virtual IEnumerable<IMethod> GetAccessors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            return EmptyList<IMethod>.Instance;
        }

        public TypeParameterSubstitution GetSubstitution()
        {
            return TypeParameterSubstitution.Identity;
        }

        public TypeParameterSubstitution GetSubstitution(IList<IType> methodTypeArguments)
        {
            return TypeParameterSubstitution.Identity;
        }

        public override sealed bool Equals(object obj)
        {
            return Equals(obj as IType);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public virtual bool Equals(IType other)
        {
            return this == other; // use reference equality by default
        }

        public override string ToString()
        {
            return this.ReflectionName;
        }

        public virtual IType AcceptVisitor(TypeVisitor visitor)
        {
            return visitor.VisitOtherType(this);
        }

        public virtual IType VisitChildren(TypeVisitor visitor)
        {
            return this;
        }
    }
    /// <summary>
    /// Contains static implementations of special types.
    /// </summary>
    [Serializable]
    public sealed class SpecialType : AbstractType, ITypeReference
    {
        /// <summary>
        /// Gets the type representing resolve errors.
        /// </summary>
        public readonly static SpecialType UnknownType = new SpecialType(TypeKind.Unknown, "?", isReferenceType: null);

        /// <summary>
        /// The null type is used as type of the null literal. It is a reference type without any members; and it is a subtype of all reference types.
        /// </summary>
        public readonly static SpecialType NullType = new SpecialType(TypeKind.Null, "null", isReferenceType: true);

        /// <summary>
        /// Type representing the C# 'dynamic' type.
        /// </summary>
        public readonly static SpecialType Dynamic = new SpecialType(TypeKind.Dynamic, "dynamic", isReferenceType: true);

        /// <summary>
        /// Type representing the result of the C# '__arglist()' expression.
        /// </summary>
        public readonly static SpecialType ArgList = new SpecialType(TypeKind.ArgList, "__arglist", isReferenceType: null);

        /// <summary>
        /// A type used for unbound type arguments in partially parameterized types.
        /// </summary>
        /// <see cref="IType.GetNestedTypes(Predicate{ITypeDefinition}, GetMemberOptions)"/>
        public readonly static SpecialType UnboundTypeArgument = new SpecialType(TypeKind.UnboundTypeArgument, "", isReferenceType: null);

        readonly TypeKind kind;
        readonly string name;
        readonly bool? isReferenceType;

        private SpecialType(TypeKind kind, string name, bool? isReferenceType)
        {
            this.kind = kind;
            this.name = name;
            this.isReferenceType = isReferenceType;
        }

        public override ITypeReference ToTypeReference()
        {
            return this;
        }

        public override string Name
        {
            get { return name; }
        }

        public override TypeKind Kind
        {
            get { return kind; }
        }

        public override bool? IsReferenceType
        {
            get { return isReferenceType; }
        }

        IType ITypeReference.Resolve(ITypeResolveContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            return this;
        }

#pragma warning disable 809
        [Obsolete("Please compare special types using the kind property instead.")]
        public override bool Equals(IType other)
        {
            // We consider a special types equal when they have equal types.
            // However, an unknown type with additional information is not considered to be equal to the SpecialType with TypeKind.Unknown.
            return other is SpecialType && other.Kind == kind;
        }

        public override int GetHashCode()
        {
            return 81625621 ^ (int)kind;
        }
    }
    /// <summary>
    /// Interface for TypeSystem objects that support interning.
    /// See <see cref="InterningProvider"/> for more information.
    /// </summary>
    public interface ISupportsInterning
    {
        /// <summary>
        /// Gets a hash code for interning.
        /// </summary>
        int GetHashCodeForInterning();

        /// <summary>
        /// Equality test for interning.
        /// </summary>
        bool EqualsForInterning(ISupportsInterning other);
    }
    /// <summary>
    /// ParameterizedType represents an instance of a generic type.
    /// Example: List&lt;string&gt;
    /// </summary>
    /// <remarks>
    /// When getting the members, this type modifies the lists so that
    /// type parameters in the signatures of the members are replaced with
    /// the type arguments.
    /// </remarks>
    [Serializable]
    public sealed class ParameterizedType : IType, ICompilationProvider
    {
        readonly ITypeDefinition genericType;
        readonly IType[] typeArguments;

        public ParameterizedType(ITypeDefinition genericType, IEnumerable<IType> typeArguments)
        {
            if (genericType == null)
                throw new ArgumentNullException("genericType");
            if (typeArguments == null)
                throw new ArgumentNullException("typeArguments");
            this.genericType = genericType;
            this.typeArguments = typeArguments.ToArray(); // copy input array to ensure it isn't modified
            if (this.typeArguments.Length == 0)
                throw new ArgumentException("Cannot use ParameterizedType with 0 type arguments.");
            if (genericType.TypeParameterCount != this.typeArguments.Length)
                throw new ArgumentException("Number of type arguments must match the type definition's number of type parameters");
            for (int i = 0; i < this.typeArguments.Length; i++)
            {
                if (this.typeArguments[i] == null)
                    throw new ArgumentNullException("typeArguments[" + i + "]");
                ICompilationProvider p = this.typeArguments[i] as ICompilationProvider;
                if (p != null && p.Compilation != genericType.Compilation)
                    throw new InvalidOperationException("Cannot parameterize a type with type arguments from a different compilation.");
            }
        }

        /// <summary>
        /// Fast internal version of the constructor. (no safety checks)
        /// Keeps the array that was passed and assumes it won't be modified.
        /// </summary>
        internal ParameterizedType(ITypeDefinition genericType, IType[] typeArguments)
        {
            Debug.Assert(genericType.TypeParameterCount == typeArguments.Length);
            this.genericType = genericType;
            this.typeArguments = typeArguments;
        }

        public TypeKind Kind
        {
            get { return genericType.Kind; }
        }

        public ICompilation Compilation
        {
            get { return genericType.Compilation; }
        }

        public bool? IsReferenceType
        {
            get { return genericType.IsReferenceType; }
        }

        public IType DeclaringType
        {
            get
            {
                ITypeDefinition declaringTypeDef = genericType.DeclaringTypeDefinition;
                if (declaringTypeDef != null && declaringTypeDef.TypeParameterCount > 0
                    && declaringTypeDef.TypeParameterCount <= genericType.TypeParameterCount)
                {
                    IType[] newTypeArgs = new IType[declaringTypeDef.TypeParameterCount];
                    Array.Copy(this.typeArguments, 0, newTypeArgs, 0, newTypeArgs.Length);
                    return new ParameterizedType(declaringTypeDef, newTypeArgs);
                }
                return declaringTypeDef;
            }
        }

        public int TypeParameterCount
        {
            get { return typeArguments.Length; }
        }

        public string FullName
        {
            get { return genericType.FullName; }
        }

        public string Name
        {
            get { return genericType.Name; }
        }

        public string Namespace
        {
            get { return genericType.Namespace; }
        }

        public string ReflectionName
        {
            get
            {
                StringBuilder b = new StringBuilder(genericType.ReflectionName);
                b.Append('[');
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    if (i > 0)
                        b.Append(',');
                    b.Append('[');
                    b.Append(typeArguments[i].ReflectionName);
                    b.Append(']');
                }
                b.Append(']');
                return b.ToString();
            }
        }

        public override string ToString()
        {
            return ReflectionName;
        }

        public IList<IType> TypeArguments
        {
            get
            {
                return typeArguments;
            }
        }

        public bool IsParameterized
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// ParameterizedTypeReference is a reference to generic class that specifies the type parameters.
        /// Example: List&lt;string&gt;
        /// </summary>
        [Serializable]
        public sealed class ParameterizedTypeReference : ITypeReference, ISupportsInterning
        {
            readonly ITypeReference genericType;
            readonly ITypeReference[] typeArguments;

            public ParameterizedTypeReference(ITypeReference genericType, IEnumerable<ITypeReference> typeArguments)
            {
                if (genericType == null)
                    throw new ArgumentNullException("genericType");
                if (typeArguments == null)
                    throw new ArgumentNullException("typeArguments");
                this.genericType = genericType;
                this.typeArguments = typeArguments.ToArray();
                for (int i = 0; i < this.typeArguments.Length; i++)
                {
                    if (this.typeArguments[i] == null)
                        throw new ArgumentNullException("typeArguments[" + i + "]");
                }
            }

            public ITypeReference GenericType
            {
                get { return genericType; }
            }

            public ReadOnlyCollection<ITypeReference> TypeArguments
            {
                get
                {
                    return Array.AsReadOnly(typeArguments);
                }
            }

            public IType Resolve(ITypeResolveContext context)
            {
                IType baseType = genericType.Resolve(context);
                ITypeDefinition baseTypeDef = baseType.GetDefinition();
                if (baseTypeDef == null)
                    return baseType;
                int tpc = baseTypeDef.TypeParameterCount;
                if (tpc == 0)
                    return baseTypeDef;
                IType[] resolvedTypes = new IType[tpc];
                for (int i = 0; i < resolvedTypes.Length; i++)
                {
                    if (i < typeArguments.Length)
                        resolvedTypes[i] = typeArguments[i].Resolve(context);
                    else
                        resolvedTypes[i] = SpecialType.UnknownType;
                }
                return new ParameterizedType(baseTypeDef, resolvedTypes);
            }

            public override string ToString()
            {
                StringBuilder b = new StringBuilder(genericType.ToString());
                b.Append('[');
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    if (i > 0)
                        b.Append(',');
                    b.Append('[');
                    b.Append(typeArguments[i].ToString());
                    b.Append(']');
                }
                b.Append(']');
                return b.ToString();
            }

            int ISupportsInterning.GetHashCodeForInterning()
            {
                int hashCode = genericType.GetHashCode();
                unchecked
                {
                    foreach (ITypeReference t in typeArguments)
                    {
                        hashCode *= 27;
                        hashCode += t.GetHashCode();
                    }
                }
                return hashCode;
            }

            bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
            {
                ParameterizedTypeReference o = other as ParameterizedTypeReference;
                if (o != null && genericType == o.genericType && typeArguments.Length == o.typeArguments.Length)
                {
                    for (int i = 0; i < typeArguments.Length; i++)
                    {
                        if (typeArguments[i] != o.typeArguments[i])
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Same as 'parameterizedType.TypeArguments[index]', but is a bit more efficient (doesn't require the read-only wrapper).
        /// </summary>
        public IType GetTypeArgument(int index)
        {
            return typeArguments[index];
        }

        /// <summary>
        /// Gets the definition of the generic type.
        /// For <c>ParameterizedType</c>, this method never returns null.
        /// </summary>
        public ITypeDefinition GetDefinition()
        {
            return genericType;
        }

        public ITypeReference ToTypeReference()
        {
            return new ParameterizedTypeReference(genericType.ToTypeReference(), typeArguments.Select(t => t.ToTypeReference()));
        }

        /// <summary>
        /// Gets a type visitor that performs the substitution of class type parameters with the type arguments
        /// of this parameterized type.
        /// </summary>
        public TypeParameterSubstitution GetSubstitution()
        {
            return new TypeParameterSubstitution(typeArguments, null);
        }

        /// <summary>
        /// Gets a type visitor that performs the substitution of class type parameters with the type arguments
        /// of this parameterized type,
        /// and also substitutes method type parameters with the specified method type arguments.
        /// </summary>
        public TypeParameterSubstitution GetSubstitution(IList<IType> methodTypeArguments)
        {
            return new TypeParameterSubstitution(typeArguments, methodTypeArguments);
        }

        public IEnumerable<IType> DirectBaseTypes
        {
            get
            {
                var substitution = GetSubstitution();
                return genericType.DirectBaseTypes.Select(t => t.AcceptVisitor(substitution));
            }
        }

        public IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetNestedTypes(filter, options);
            else
                return GetMembersHelper.GetNestedTypes(this, filter, options);
        }

        public IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetNestedTypes(typeArguments, filter, options);
            else
                return GetMembersHelper.GetNestedTypes(this, typeArguments, filter, options);
        }

        public IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetConstructors(filter, options);
            else
                return GetMembersHelper.GetConstructors(this, filter, options);
        }

        public IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetMethods(filter, options);
            else
                return GetMembersHelper.GetMethods(this, filter, options);
        }

        public IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetMethods(typeArguments, filter, options);
            else
                return GetMembersHelper.GetMethods(this, typeArguments, filter, options);
        }

        public IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetProperties(filter, options);
            else
                return GetMembersHelper.GetProperties(this, filter, options);
        }

        public IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetFields(filter, options);
            else
                return GetMembersHelper.GetFields(this, filter, options);
        }

        public IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetEvents(filter, options);
            else
                return GetMembersHelper.GetEvents(this, filter, options);
        }

        public IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetMembers(filter, options);
            else
                return GetMembersHelper.GetMembers(this, filter, options);
        }

        public IEnumerable<IMethod> GetAccessors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                return genericType.GetAccessors(filter, options);
            else
                return GetMembersHelper.GetAccessors(this, filter, options);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IType);
        }

        public bool Equals(IType other)
        {
            ParameterizedType c = other as ParameterizedType;
            if (c == null || !genericType.Equals(c.genericType) || typeArguments.Length != c.typeArguments.Length)
                return false;
            for (int i = 0; i < typeArguments.Length; i++)
            {
                if (!typeArguments[i].Equals(c.typeArguments[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = genericType.GetHashCode();
            unchecked
            {
                foreach (var ta in typeArguments)
                {
                    hashCode *= 1000000007;
                    hashCode += 1000000009 * ta.GetHashCode();
                }
            }
            return hashCode;
        }

        public IType AcceptVisitor(TypeVisitor visitor)
        {
            return visitor.VisitParameterizedType(this);
        }

        public IType VisitChildren(TypeVisitor visitor)
        {
            IType g = genericType.AcceptVisitor(visitor);
            ITypeDefinition def = g as ITypeDefinition;
            if (def == null)
                return g;
            // Keep ta == null as long as no elements changed, allocate the array only if necessary.
            IType[] ta = (g != genericType) ? new IType[typeArguments.Length] : null;
            for (int i = 0; i < typeArguments.Length; i++)
            {
                IType r = typeArguments[i].AcceptVisitor(visitor);
                if (r == null)
                    throw new NullReferenceException("TypeVisitor.Visit-method returned null");
                if (ta == null && r != typeArguments[i])
                {
                    // we found a difference, so we need to allocate the array
                    ta = new IType[typeArguments.Length];
                    for (int j = 0; j < i; j++)
                    {
                        ta[j] = typeArguments[j];
                    }
                }
                if (ta != null)
                    ta[i] = r;
            }
            if (def == genericType && ta == null)
                return this;
            else
                return new ParameterizedType(def, ta ?? typeArguments);
        }
    }
    /// <summary>
    /// Represents the variance of a type parameter.
    /// </summary>
    public enum VarianceModifier : byte
    {
        /// <summary>
        /// The type parameter is not variant.
        /// </summary>
        Invariant,
        /// <summary>
        /// The type parameter is covariant (used in output position).
        /// </summary>
        Covariant,
        /// <summary>
        /// The type parameter is contravariant (used in input position).
        /// </summary>
        Contravariant
    };

    /// <summary>
    /// Type parameter of a generic class/method.
    /// </summary>
    public interface ITypeParameter : IType, ISymbol
    {
        /// <summary>
        /// Get the type of this type parameter's owner.
        /// </summary>
        /// <returns>SymbolKind.TypeDefinition or SymbolKind.Method</returns>
        SymbolKind OwnerType { get; }

        /// <summary>
        /// Gets the owning method/class.
        /// This property may return null (for example for the dummy type parameters used by <see cref="ParameterListComparer.NormalizeMethodTypeParameters"/>).
        /// </summary>
        /// <remarks>
        /// For "class Outer&lt;T&gt; { class Inner {} }",
        /// inner.TypeParameters[0].Owner will be the outer class, because the same
        /// ITypeParameter instance is used both on Outer`1 and Outer`1+Inner.
        /// </remarks>
        IEntity Owner { get; }

        /// <summary>
        /// Gets the index of the type parameter in the type parameter list of the owning method/class.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the name of the type parameter.
        /// </summary>
        new string Name { get; }

        /// <summary>
        /// Gets the list of attributes declared on this type parameter.
        /// </summary>
        IList<IAttribute> Attributes { get; }

        /// <summary>
        /// Gets the variance of this type parameter.
        /// </summary>
        VarianceModifier Variance { get; }

        /// <summary>
        /// Gets the region where the type parameter is defined.
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Gets the effective base class of this type parameter.
        /// </summary>
        IType EffectiveBaseClass { get; }

        /// <summary>
        /// Gets the effective interface set of this type parameter.
        /// </summary>
        ICollection<IType> EffectiveInterfaceSet { get; }

        /// <summary>
        /// Gets if the type parameter has the 'new()' constraint.
        /// </summary>
        bool HasDefaultConstructorConstraint { get; }

        /// <summary>
        /// Gets if the type parameter has the 'class' constraint.
        /// </summary>
        bool HasReferenceTypeConstraint { get; }

        /// <summary>
        /// Gets if the type parameter has the 'struct' constraint.
        /// </summary>
        bool HasValueTypeConstraint { get; }
    }

    public abstract class TypeWithElementType : AbstractType
    {
        [CLSCompliant(false)]
        protected IType elementType;

        protected TypeWithElementType(IType elementType)
        {
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            this.elementType = elementType;
        }

        public override string Name
        {
            get { return elementType.Name + NameSuffix; }
        }

        public override string Namespace
        {
            get { return elementType.Namespace; }
        }

        public override string FullName
        {
            get { return elementType.FullName + NameSuffix; }
        }

        public override string ReflectionName
        {
            get { return elementType.ReflectionName + NameSuffix; }
        }

        public abstract string NameSuffix { get; }

        public IType ElementType
        {
            get { return elementType; }
        }

        // Force concrete implementations to override VisitChildren - the base implementation
        // in AbstractType assumes there are no children, but we know there is (at least) 1.
        public abstract override IType VisitChildren(TypeVisitor visitor);
    }

    [Serializable]
    public sealed class ArrayTypeReference : ITypeReference, ISupportsInterning
    {
        readonly ITypeReference elementType;
        readonly int dimensions;

        public ArrayTypeReference(ITypeReference elementType, int dimensions = 1)
        {
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            if (dimensions <= 0)
                throw new ArgumentOutOfRangeException("dimensions", dimensions, "dimensions must be positive");
            this.elementType = elementType;
            this.dimensions = dimensions;
        }

        public ITypeReference ElementType
        {
            get { return elementType; }
        }

        public int Dimensions
        {
            get { return dimensions; }
        }

        public IType Resolve(ITypeResolveContext context)
        {
            return new ArrayType(context.Compilation, elementType.Resolve(context), dimensions);
        }

        public override string ToString()
        {
            return elementType.ToString() + "[" + new string(',', dimensions - 1) + "]";
        }

        int ISupportsInterning.GetHashCodeForInterning()
        {
            return elementType.GetHashCode() ^ dimensions;
        }

        bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
        {
            ArrayTypeReference o = other as ArrayTypeReference;
            return o != null && elementType == o.elementType && dimensions == o.dimensions;
        }
    }

    /// <summary>
    /// Represents an array type.
    /// </summary>
    public sealed class ArrayType : TypeWithElementType, ICompilationProvider
    {
        readonly int dimensions;
        readonly ICompilation compilation;

        public ArrayType(ICompilation compilation, IType elementType, int dimensions = 1) : base(elementType)
        {
            if (compilation == null)
                throw new ArgumentNullException("compilation");
            if (dimensions <= 0)
                throw new ArgumentOutOfRangeException("dimensions", dimensions, "dimensions must be positive");
            this.compilation = compilation;
            this.dimensions = dimensions;

            ICompilationProvider p = elementType as ICompilationProvider;
            if (p != null && p.Compilation != compilation)
                throw new InvalidOperationException("Cannot create an array type using a different compilation from the element type.");
        }

        public override TypeKind Kind
        {
            get { return TypeKind.Array; }
        }

        public ICompilation Compilation
        {
            get { return compilation; }
        }

        public int Dimensions
        {
            get { return dimensions; }
        }

        public override string NameSuffix
        {
            get
            {
                return "[" + new string(',', dimensions - 1) + "]";
            }
        }

        public override bool? IsReferenceType
        {
            get { return true; }
        }

        public override int GetHashCode()
        {
            return unchecked(elementType.GetHashCode() * 71681 + dimensions);
        }

        public override bool Equals(IType other)
        {
            ArrayType a = other as ArrayType;
            return a != null && elementType.Equals(a.elementType) && a.dimensions == dimensions;
        }

        public override ITypeReference ToTypeReference()
        {
            return new ArrayTypeReference(elementType.ToTypeReference(), dimensions);
        }

        public override IEnumerable<IType> DirectBaseTypes
        {
            get
            {
                List<IType> baseTypes = new List<IType>();
                IType t = compilation.FindType(KnownTypeCode.Array);
                if (t.Kind != TypeKind.Unknown)
                    baseTypes.Add(t);
                if (dimensions == 1 && elementType.Kind != TypeKind.Pointer)
                {
                    // single-dimensional arrays implement IList<T>
                    ITypeDefinition def = compilation.FindType(KnownTypeCode.IListOfT) as ITypeDefinition;
                    if (def != null)
                        baseTypes.Add(new ParameterizedType(def, new[] { elementType }));
                    // And in .NET 4.5 they also implement IReadOnlyList<T>
                    def = compilation.FindType(KnownTypeCode.IReadOnlyListOfT) as ITypeDefinition;
                    if (def != null)
                        baseTypes.Add(new ParameterizedType(def, new[] { elementType }));
                }
                return baseTypes;
            }
        }

        public override IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
                return EmptyList<IMethod>.Instance;
            else
                return compilation.FindType(KnownTypeCode.Array).GetMethods(filter, options);
        }

        public override IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
                return EmptyList<IMethod>.Instance;
            else
                return compilation.FindType(KnownTypeCode.Array).GetMethods(typeArguments, filter, options);
        }

        public override IEnumerable<IMethod> GetAccessors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
                return EmptyList<IMethod>.Instance;
            else
                return compilation.FindType(KnownTypeCode.Array).GetAccessors(filter, options);
        }

        public override IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
                return EmptyList<IProperty>.Instance;
            else
                return compilation.FindType(KnownTypeCode.Array).GetProperties(filter, options);
        }

        // NestedTypes, Events, Fields: System.Array doesn't have any; so we can use the AbstractType default implementation
        // that simply returns an empty list

        public override IType AcceptVisitor(TypeVisitor visitor)
        {
            return visitor.VisitArrayType(this);
        }

        public override IType VisitChildren(TypeVisitor visitor)
        {
            IType e = elementType.AcceptVisitor(visitor);
            if (e == elementType)
                return this;
            else
                return new ArrayType(compilation, e, dimensions);
        }
    }
    public sealed class PointerType : TypeWithElementType
    {
        public PointerType(IType elementType) : base(elementType)
        {
        }

        public override TypeKind Kind
        {
            get { return TypeKind.Pointer; }
        }

        public override string NameSuffix
        {
            get
            {
                return "*";
            }
        }

        public override bool? IsReferenceType
        {
            get { return null; }
        }

        public override int GetHashCode()
        {
            return elementType.GetHashCode() ^ 91725811;
        }

        public override bool Equals(IType other)
        {
            PointerType a = other as PointerType;
            return a != null && elementType.Equals(a.elementType);
        }

        public override IType AcceptVisitor(TypeVisitor visitor)
        {
            return visitor.VisitPointerType(this);
        }

        public override IType VisitChildren(TypeVisitor visitor)
        {
            IType e = elementType.AcceptVisitor(visitor);
            if (e == elementType)
                return this;
            else
                return new PointerType(e);
        }

        public override ITypeReference ToTypeReference()
        {
            return new PointerTypeReference(elementType.ToTypeReference());
        }
    }

    [Serializable]
    public sealed class PointerTypeReference : ITypeReference, ISupportsInterning
    {
        readonly ITypeReference elementType;

        public PointerTypeReference(ITypeReference elementType)
        {
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            this.elementType = elementType;
        }

        public ITypeReference ElementType
        {
            get { return elementType; }
        }

        public IType Resolve(ITypeResolveContext context)
        {
            return new PointerType(elementType.Resolve(context));
        }

        public override string ToString()
        {
            return elementType.ToString() + "*";
        }

        int ISupportsInterning.GetHashCodeForInterning()
        {
            return elementType.GetHashCode() ^ 91725812;
        }

        bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
        {
            PointerTypeReference o = other as PointerTypeReference;
            return o != null && this.elementType == o.elementType;
        }
    }

    public sealed class ByReferenceType : TypeWithElementType
    {
        public ByReferenceType(IType elementType) : base(elementType)
        {
        }

        public override TypeKind Kind
        {
            get { return TypeKind.ByReference; }
        }

        public override string NameSuffix
        {
            get
            {
                return "&";
            }
        }

        public override bool? IsReferenceType
        {
            get { return null; }
        }

        public override int GetHashCode()
        {
            return elementType.GetHashCode() ^ 91725813;
        }

        public override bool Equals(IType other)
        {
            ByReferenceType a = other as ByReferenceType;
            return a != null && elementType.Equals(a.elementType);
        }

        public override IType AcceptVisitor(TypeVisitor visitor)
        {
            return visitor.VisitByReferenceType(this);
        }

        public override IType VisitChildren(TypeVisitor visitor)
        {
            IType e = elementType.AcceptVisitor(visitor);
            if (e == elementType)
                return this;
            else
                return new ByReferenceType(e);
        }

        public override ITypeReference ToTypeReference()
        {
            return new ByReferenceTypeReference(elementType.ToTypeReference());
        }
    }

    [Serializable]
    public sealed class ByReferenceTypeReference : ITypeReference, ISupportsInterning
    {
        readonly ITypeReference elementType;

        public ByReferenceTypeReference(ITypeReference elementType)
        {
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            this.elementType = elementType;
        }

        public ITypeReference ElementType
        {
            get { return elementType; }
        }

        public IType Resolve(ITypeResolveContext context)
        {
            return new ByReferenceType(elementType.Resolve(context));
        }

        public override string ToString()
        {
            return elementType.ToString() + "&";
        }

        int ISupportsInterning.GetHashCodeForInterning()
        {
            return elementType.GetHashCode() ^ 91725814;
        }

        bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
        {
            ByReferenceTypeReference brt = other as ByReferenceTypeReference;
            return brt != null && this.elementType == brt.elementType;
        }
    }
    /// <summary>
    /// Base class for the visitor pattern on <see cref="IType"/>.
    /// </summary>
    public abstract class TypeVisitor
    {
        public virtual IType VisitTypeDefinition(ITypeDefinition type)
        {
            return type.VisitChildren(this);
        }

        public virtual IType VisitTypeParameter(ITypeParameter type)
        {
            return type.VisitChildren(this);
        }

        public virtual IType VisitParameterizedType(ParameterizedType type)
        {
            return type.VisitChildren(this);
        }

        public virtual IType VisitArrayType(ArrayType type)
        {
            return type.VisitChildren(this);
        }

        public virtual IType VisitPointerType(PointerType type)
        {
            return type.VisitChildren(this);
        }

        public virtual IType VisitByReferenceType(ByReferenceType type)
        {
            return type.VisitChildren(this);
        }

        public virtual IType VisitOtherType(IType type)
        {
            return type.VisitChildren(this);
        }
    }
    /// <summary>
    /// Substitutes class and method type parameters.
    /// </summary>
    public class TypeParameterSubstitution : TypeVisitor
    {
        /// <summary>
        /// The identity function.
        /// </summary>
        public static readonly TypeParameterSubstitution Identity = new TypeParameterSubstitution(null, null);

        readonly IList<IType> classTypeArguments;
        readonly IList<IType> methodTypeArguments;

        /// <summary>
        /// Creates a new type parameter substitution.
        /// </summary>
        /// <param name="classTypeArguments">
        /// The type arguments to substitute for class type parameters.
        /// Pass <c>null</c> to keep class type parameters unmodified.
        /// </param>
        /// <param name="methodTypeArguments">
        /// The type arguments to substitute for method type parameters.
        /// Pass <c>null</c> to keep method type parameters unmodified.
        /// </param>
        public TypeParameterSubstitution(IList<IType> classTypeArguments, IList<IType> methodTypeArguments)
        {
            this.classTypeArguments = classTypeArguments;
            this.methodTypeArguments = methodTypeArguments;
        }

        /// <summary>
        /// Gets the list of class type arguments.
        /// Returns <c>null</c> if this substitution keeps class type parameters unmodified.
        /// </summary>
        public IList<IType> ClassTypeArguments
        {
            get { return classTypeArguments; }
        }

        /// <summary>
        /// Gets the list of method type arguments.
        /// Returns <c>null</c> if this substitution keeps method type parameters unmodified.
        /// </summary>
        public IList<IType> MethodTypeArguments
        {
            get { return methodTypeArguments; }
        }

        #region Compose
        /// <summary>
        /// Computes a single TypeParameterSubstitution so that for all types <c>t</c>:
        /// <c>t.AcceptVisitor(Compose(g, f)) equals t.AcceptVisitor(f).AcceptVisitor(g)</c>
        /// </summary>
        /// <remarks>If you consider type parameter substitution to be a function, this is function composition.</remarks>
        public static TypeParameterSubstitution Compose(TypeParameterSubstitution g, TypeParameterSubstitution f)
        {
            if (g == null)
                return f;
            if (f == null || (f.classTypeArguments == null && f.methodTypeArguments == null))
                return g;
            // The composition is a copy of 'f', with 'g' applied on the array elements.
            // If 'f' has a null list (keeps type parameters unmodified), we have to treat it as
            // the identity function, and thus use the list from 'g'.
            var classTypeArguments = f.classTypeArguments != null ? GetComposedTypeArguments(f.classTypeArguments, g) : g.classTypeArguments;
            var methodTypeArguments = f.methodTypeArguments != null ? GetComposedTypeArguments(f.methodTypeArguments, g) : g.methodTypeArguments;
            return new TypeParameterSubstitution(classTypeArguments, methodTypeArguments);
        }

        static IList<IType> GetComposedTypeArguments(IList<IType> input, TypeParameterSubstitution substitution)
        {
            IType[] result = new IType[input.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = input[i].AcceptVisitor(substitution);
            }
            return result;
        }
        #endregion

        #region Equals and GetHashCode implementation
        public override bool Equals(object obj)
        {
            TypeParameterSubstitution other = obj as TypeParameterSubstitution;
            if (other == null)
                return false;
            return TypeListEquals(classTypeArguments, other.classTypeArguments)
                && TypeListEquals(methodTypeArguments, other.methodTypeArguments);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return 1124131 * TypeListHashCode(classTypeArguments) + 1821779 * TypeListHashCode(methodTypeArguments);
            }
        }

        static bool TypeListEquals(IList<IType> a, IList<IType> b)
        {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }
            return true;
        }

        static int TypeListHashCode(IList<IType> obj)
        {
            if (obj == null)
                return 0;
            unchecked
            {
                int hashCode = 1;
                foreach (var element in obj)
                {
                    hashCode *= 27;
                    hashCode += element.GetHashCode();
                }
                return hashCode;
            }
        }
        #endregion

        public override IType VisitTypeParameter(ITypeParameter type)
        {
            int index = type.Index;
            if (classTypeArguments != null && type.OwnerType == SymbolKind.TypeDefinition)
            {
                if (index >= 0 && index < classTypeArguments.Count)
                    return classTypeArguments[index];
                else
                    return SpecialType.UnknownType;
            }
            else if (methodTypeArguments != null && type.OwnerType == SymbolKind.Method)
            {
                if (index >= 0 && index < methodTypeArguments.Count)
                    return methodTypeArguments[index];
                else
                    return SpecialType.UnknownType;
            }
            else
            {
                return base.VisitTypeParameter(type);
            }
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append('[');
            bool first = true;
            if (classTypeArguments != null)
            {
                for (int i = 0; i < classTypeArguments.Count; i++)
                {
                    if (first) first = false; else b.Append(", ");
                    b.Append('`');
                    b.Append(i);
                    b.Append(" -> ");
                    b.Append(classTypeArguments[i].ReflectionName);
                }
            }
            if (methodTypeArguments != null)
            {
                for (int i = 0; i < methodTypeArguments.Count; i++)
                {
                    if (first) first = false; else b.Append(", ");
                    b.Append("``");
                    b.Append(i);
                    b.Append(" -> ");
                    b.Append(methodTypeArguments[i].ReflectionName);
                }
            }
            b.Append(']');
            return b.ToString();
        }
    }

    public interface ISymbolReference
    {
        ISymbol Resolve(ITypeResolveContext context);
    }
    public interface IMemberReference : ISymbolReference
    {
        /// <summary>
        /// Gets the declaring type reference for the member.
        /// </summary>
        ITypeReference DeclaringTypeReference { get; }

        /// <summary>
        /// Resolves the member.
        /// </summary>
        /// <param name="context">
        /// Context to use for resolving this member reference.
        /// Which kind of context is required depends on the which kind of member reference this is;
        /// please consult the documentation of the method that was used to create this member reference,
        /// or that of the class implementing this method.
        /// </param>
        /// <returns>
        /// Returns the resolved member, or <c>null</c> if the member could not be found.
        /// </returns>
        new IMember Resolve(ITypeResolveContext context);
    }

    /// <summary>
    /// Method/field/property/event.
    /// </summary>
    public interface IUnresolvedMember : IUnresolvedEntity, IMemberReference
    {
        /// <summary>
        /// Gets the return type of this member.
        /// This property never returns null.
        /// </summary>
        ITypeReference ReturnType { get; }

        /// <summary>
        /// Gets whether this member is explicitly implementing an interface.
        /// If this property is true, the member can only be called through the interfaces it implements.
        /// </summary>
        bool IsExplicitInterfaceImplementation { get; }

        /// <summary>
        /// Gets the interfaces that are explicitly implemented by this member.
        /// </summary>
        IList<IMemberReference> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Gets if the member is virtual. Is true only if the "virtual" modifier was used, but non-virtual
        /// members can be overridden, too; if they are abstract or overriding a method.
        /// </summary>
        bool IsVirtual { get; }

        /// <summary>
        /// Gets whether this member is overriding another member.
        /// </summary>
        bool IsOverride { get; }

        /// <summary>
        /// Gets if the member can be overridden. Returns true when the member is "abstract", "virtual" or "override" but not "sealed".
        /// </summary>
        bool IsOverridable { get; }

        /// <summary>
        /// Resolves the member.
        /// </summary>
        /// <param name="context">
        /// Context for looking up the member. The context must specify the current assembly.
        /// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
        /// </param>
        /// <returns>
        /// Returns the resolved member, or <c>null</c> if the member could not be found.
        /// </returns>
        new IMember Resolve(ITypeResolveContext context);

        /// <summary>
        /// Creates the resolved member.
        /// </summary>
        /// <param name="context">
        /// The language-specific context that includes the parent type definition.
        /// <see cref="IUnresolvedTypeDefinition.CreateResolveContext"/>
        /// </param>
        IMember CreateResolved(ITypeResolveContext context);
    }
    /// <summary>
    /// Method/field/property/event.
    /// </summary>
    public interface IMember : IEntity
    {
        /// <summary>
        /// Gets the original member definition for this member.
        /// Returns <c>this</c> if this is not a specialized member.
        /// Specialized members are the result of overload resolution with type substitution.
        /// </summary>
        IMember MemberDefinition { get; }

        /// <summary>
        /// Gets the unresolved member instance from which this member was created.
        /// This property may return <c>null</c> for special members that do not have a corresponding unresolved member instance.
        /// </summary>
        /// <remarks>
        /// For specialized members, this property returns the unresolved member for the original member definition.
        /// For partial methods, this property returns the implementing partial method declaration, if one exists, and the
        /// defining partial method declaration otherwise.
        /// For the members used to represent the built-in C# operators like "operator +(int, int);", this property returns <c>null</c>.
        /// </remarks>
        IUnresolvedMember UnresolvedMember { get; }

        /// <summary>
        /// Gets the return type of this member.
        /// This property never returns <c>null</c>.
        /// </summary>
        IType ReturnType { get; }

        /// <summary>
        /// Gets the interface members implemented by this member (both implicitly and explicitly).
        /// </summary>
        IList<IMember> ImplementedInterfaceMembers { get; }

        /// <summary>
        /// Gets whether this member is explicitly implementing an interface.
        /// </summary>
        bool IsExplicitInterfaceImplementation { get; }

        /// <summary>
        /// Gets if the member is virtual. Is true only if the "virtual" modifier was used, but non-virtual
        /// members can be overridden, too; if they are abstract or overriding a method.
        /// </summary>
        bool IsVirtual { get; }

        /// <summary>
        /// Gets whether this member is overriding another member.
        /// </summary>
        bool IsOverride { get; }

        /// <summary>
        /// Gets if the member can be overridden. Returns true when the member is "abstract", "virtual" or "override" but not "sealed".
        /// </summary>
        bool IsOverridable { get; }

        /// <summary>
        /// Creates a member reference that can be used to rediscover this member in another compilation.
        /// </summary>
        /// <remarks>
        /// If this member is specialized using open generic types, the resulting member reference will need to be looked up in an appropriate generic context.
        /// Otherwise, the main resolve context of a compilation is sufficient.
        /// </remarks>
        [Obsolete("Use the ToReference method instead.")]
        IMemberReference ToMemberReference();

        /// <summary>
        /// Creates a member reference that can be used to rediscover this member in another compilation.
        /// </summary>
        /// <remarks>
        /// If this member is specialized using open generic types, the resulting member reference will need to be looked up in an appropriate generic context.
        /// Otherwise, the main resolve context of a compilation is sufficient.
        /// </remarks>
        new IMemberReference ToReference();

        /// <summary>
        /// Gets the substitution belonging to this specialized member.
        /// Returns TypeParameterSubstitution.Identity for not specialized members.
        /// </summary>
        TypeParameterSubstitution Substitution
        {
            get;
        }

        /// <summary>
        /// Specializes this member with the given substitution.
        /// If this member is already specialized, the new substitution is composed with the existing substition.
        /// </summary>
        IMember Specialize(TypeParameterSubstitution substitution);
    }
    /// <summary>
    /// Represents a method or property.
    /// </summary>
    public interface IParameterizedMember : IMember
    {
        IList<IParameter> Parameters { get; }
    }

    /// <summary>
    /// Represents a method, constructor, destructor or operator.
    /// </summary>
    public interface IMethod : IParameterizedMember
    {
        /// <summary>
        /// Gets the unresolved method parts.
        /// For partial methods, this returns all parts.
        /// Otherwise, this returns an array with a single element (new[] { UnresolvedMember }).
        /// NOTE: The type will change to IReadOnlyList&lt;IUnresolvedMethod&gt; in future versions.
        /// </summary>
        IList<IUnresolvedMethod> Parts { get; }

        /// <summary>
        /// Gets the attributes associated with the return type. (e.g. [return: MarshalAs(...)])
        /// NOTE: The type will change to IReadOnlyList&lt;IAttribute&gt; in future versions.
        /// </summary>
        IList<IAttribute> ReturnTypeAttributes { get; }

        /// <summary>
        /// Gets the type parameters of this method; or an empty list if the method is not generic.
        /// NOTE: The type will change to IReadOnlyList&lt;ITypeParameter&gt; in future versions.
        /// </summary>
        IList<ITypeParameter> TypeParameters { get; }

        /// <summary>
        /// Gets whether this is a generic method that has been parameterized.
        /// </summary>
        bool IsParameterized { get; }

        /// <summary>
        /// Gets the type arguments passed to this method.
        /// If the method is generic but not parameterized yet, this property returns the type parameters,
        /// as if the method was parameterized with its own type arguments (<c>void M&lt;T&gt;() { M&lt;T&gt;(); }</c>).
        /// 
        /// NOTE: The type will change to IReadOnlyList&lt;IType&gt; in future versions.
        /// </summary>
        IList<IType> TypeArguments { get; }

        bool IsExtensionMethod { get; }
        bool IsConstructor { get; }
        bool IsDestructor { get; }
        bool IsOperator { get; }

        /// <summary>
        /// Gets whether the method is a C#-style partial method.
        /// A call to such a method is ignored by the compiler if the partial method has no body.
        /// </summary>
        /// <seealso cref="HasBody"/>
        bool IsPartial { get; }

        /// <summary>
        /// Gets whether the method is a C#-style async method.
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Gets whether the method has a body.
        /// This property returns <c>false</c> for <c>abstract</c> or <c>extern</c> methods,
        /// or for <c>partial</c> methods without implementation.
        /// </summary>
        bool HasBody { get; }

        /// <summary>
        /// Gets whether the method is a property/event accessor.
        /// </summary>
        bool IsAccessor { get; }

        /// <summary>
        /// If this method is an accessor, returns the corresponding property/event.
        /// Otherwise, returns null.
        /// </summary>
        IMember AccessorOwner { get; }

        /// <summary>
        /// If this method is reduced from an extension method return the original method, <c>null</c> otherwise.
        /// A reduced method doesn't contain the extension method parameter. That means that has one parameter less than it's definition.
        /// </summary>
        IMethod ReducedFrom { get; }

        /// <summary>
        /// Specializes this method with the given substitution.
        /// If this method is already specialized, the new substitution is composed with the existing substition.
        /// </summary>
        new IMethod Specialize(TypeParameterSubstitution substitution);
    }
    /// <summary>
    /// Represents an attribute.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IAttribute
    {
        /// <summary>
        /// Gets the code region of this attribute.
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Gets the type of the attribute.
        /// </summary>
        IType AttributeType { get; }

        /// <summary>
        /// Gets the constructor being used.
        /// This property may return null if no matching constructor was found.
        /// </summary>
        IMethod Constructor { get; }

        /// <summary>
        /// Gets the positional arguments.
        /// </summary>
        IList<ResolveResult> PositionalArguments { get; }

        /// <summary>
        /// Gets the named arguments passed to the attribute.
        /// </summary>
        IList<KeyValuePair<IMember, ResolveResult>> NamedArguments { get; }
    }

    /// <summary>
    /// Represents some well-known types.
    /// </summary>
    public enum KnownTypeCode
    {
        // Note: DefaultResolvedTypeDefinition uses (KnownTypeCode)-1 as special value for "not yet calculated".
        // The order of type codes at the beginning must correspond to those in System.TypeCode.

        /// <summary>
        /// Not one of the known types.
        /// </summary>
        None,
        /// <summary><c>object</c> (System.Object)</summary>
        Object,
        /// <summary><c>System.DBNull</c></summary>
        DBNull,
        /// <summary><c>bool</c> (System.Boolean)</summary>
        Boolean,
        /// <summary><c>char</c> (System.Char)</summary>
        Char,
        /// <summary><c>sbyte</c> (System.SByte)</summary>
        SByte,
        /// <summary><c>byte</c> (System.Byte)</summary>
        Byte,
        /// <summary><c>short</c> (System.Int16)</summary>
        Int16,
        /// <summary><c>ushort</c> (System.UInt16)</summary>
        UInt16,
        /// <summary><c>int</c> (System.Int32)</summary>
        Int32,
        /// <summary><c>uint</c> (System.UInt32)</summary>
        UInt32,
        /// <summary><c>long</c> (System.Int64)</summary>
        Int64,
        /// <summary><c>ulong</c> (System.UInt64)</summary>
        UInt64,
        /// <summary><c>float</c> (System.Single)</summary>
        Single,
        /// <summary><c>double</c> (System.Double)</summary>
        Double,
        /// <summary><c>decimal</c> (System.Decimal)</summary>
        Decimal,
        /// <summary><c>System.DateTime</c></summary>
        DateTime,
        /// <summary><c>string</c> (System.String)</summary>
        String = 18,

        // String was the last element from System.TypeCode, now our additional known types start

        /// <summary><c>void</c> (System.Void)</summary>
        Void,
        /// <summary><c>System.Type</c></summary>
        Type,
        /// <summary><c>System.Array</c></summary>
        Array,
        /// <summary><c>System.Attribute</c></summary>
        Attribute,
        /// <summary><c>System.ValueType</c></summary>
        ValueType,
        /// <summary><c>System.Enum</c></summary>
        Enum,
        /// <summary><c>System.Delegate</c></summary>
        Delegate,
        /// <summary><c>System.MulticastDelegate</c></summary>
        MulticastDelegate,
        /// <summary><c>System.Exception</c></summary>
        Exception,
        /// <summary><c>System.IntPtr</c></summary>
        IntPtr,
        /// <summary><c>System.UIntPtr</c></summary>
        UIntPtr,
        /// <summary><c>System.Collections.IEnumerable</c></summary>
        IEnumerable,
        /// <summary><c>System.Collections.IEnumerator</c></summary>
        IEnumerator,
        /// <summary><c>System.Collections.Generic.IEnumerable{T}</c></summary>
        IEnumerableOfT,
        /// <summary><c>System.Collections.Generic.IEnumerator{T}</c></summary>
        IEnumeratorOfT,
        /// <summary><c>System.Collections.Generic.ICollection</c></summary>
        ICollection,
        /// <summary><c>System.Collections.Generic.ICollection{T}</c></summary>
        ICollectionOfT,
        /// <summary><c>System.Collections.Generic.IList</c></summary>
        IList,
        /// <summary><c>System.Collections.Generic.IList{T}</c></summary>
        IListOfT,
        /// <summary><c>System.Collections.Generic.IReadOnlyCollection{T}</c></summary>
        IReadOnlyCollectionOfT,
        /// <summary><c>System.Collections.Generic.IReadOnlyList{T}</c></summary>
        IReadOnlyListOfT,
        /// <summary><c>System.Threading.Tasks.Task</c></summary>
        Task,
        /// <summary><c>System.Threading.Tasks.Task{T}</c></summary>
        TaskOfT,
        /// <summary><c>System.Nullable{T}</c></summary>
        NullableOfT,
        /// <summary><c>System.IDisposable</c></summary>
        IDisposable,
        /// <summary><c>System.Runtime.CompilerServices.INotifyCompletion</c></summary>
        INotifyCompletion,
        /// <summary><c>System.Runtime.CompilerServices.ICriticalNotifyCompletion</c></summary>
        ICriticalNotifyCompletion,
    }
    /// <summary>
    /// Represents a resolved namespace.
    /// </summary>
    public interface INamespace : ISymbol, ICompilationProvider
    {
        // No pointer back to unresolved namespace:
        // multiple unresolved namespaces (from different assemblies) get
        // merged into one INamespace.

        /// <summary>
        /// Gets the extern alias for this namespace.
        /// Returns an empty string for normal namespaces.
        /// </summary>
        string ExternAlias { get; }

        /// <summary>
        /// Gets the full name of this namespace. (e.g. "System.Collections")
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the short name of this namespace (e.g. "Collections").
        /// </summary>
        new string Name { get; }

        /// <summary>
        /// Gets the parent namespace.
        /// Returns null if this is the root namespace.
        /// </summary>
        INamespace ParentNamespace { get; }

        /// <summary>
        /// Gets the child namespaces in this namespace.
        /// </summary>
        IEnumerable<INamespace> ChildNamespaces { get; }

        /// <summary>
        /// Gets the types in this namespace.
        /// </summary>
        IEnumerable<ITypeDefinition> Types { get; }

        /// <summary>
        /// Gets the assemblies that contribute types to this namespace (or to child namespaces).
        /// </summary>
        IEnumerable<IAssembly> ContributingAssemblies { get; }

        /// <summary>
        /// Gets a direct child namespace by its short name.
        /// Returns null when the namespace cannot be found.
        /// </summary>
        /// <remarks>
        /// This method uses the compilation's current string comparer.
        /// </remarks>
        INamespace GetChildNamespace(string name);

        /// <summary>
        /// Gets the type with the specified short name and type parameter count.
        /// Returns null if the type cannot be found.
        /// </summary>
        /// <remarks>
        /// This method uses the compilation's current string comparer.
        /// </remarks>
        ITypeDefinition GetTypeDefinition(string name, int typeParameterCount);
    }

    /// <summary>
    /// Enum that describes the type of an error.
    /// </summary>
    public enum ErrorType
    {
        Unknown,
        Error,
        Warning
    }

    /// <summary>
    /// Descibes an error during parsing.
    /// </summary>
    [Serializable]
    public class Error
    {
        readonly ErrorType errorType;
        readonly string message;
        readonly DomRegion region;

        /// <summary>
        /// The type of the error.
        /// </summary>
        public ErrorType ErrorType { get { return errorType; } }

        /// <summary>
        /// The error description.
        /// </summary>
        public string Message { get { return message; } }

        /// <summary>
        /// The region of the error.
        /// </summary>
        public DomRegion Region { get { return region; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
        /// </summary>
        /// <param name='errorType'>
        /// The error type.
        /// </param>
        /// <param name='message'>
        /// The description of the error.
        /// </param>
        /// <param name='region'>
        /// The region of the error.
        /// </param>
        public Error(ErrorType errorType, string message, DomRegion region)
        {
            this.errorType = errorType;
            this.message = message;
            this.region = region;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
        /// </summary>
        /// <param name='errorType'>
        /// The error type.
        /// </param>
        /// <param name='message'>
        /// The description of the error.
        /// </param>
        /// <param name='location'>
        /// The location of the error.
        /// </param>
        public Error(ErrorType errorType, string message, TextLocation location)
        {
            this.errorType = errorType;
            this.message = message;
            this.region = new DomRegion(location, location);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
        /// </summary>
        /// <param name='errorType'>
        /// The error type.
        /// </param>
        /// <param name='message'>
        /// The description of the error.
        /// </param>
        /// <param name='line'>
        /// The line of the error.
        /// </param>
        /// <param name='col'>
        /// The column of the error.
        /// </param>
        public Error(ErrorType errorType, string message, int line, int col) : this(errorType, message, new TextLocation(line, col))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.Error"/> class.
        /// </summary>
        /// <param name='errorType'>
        /// The error type.
        /// </param>
        /// <param name='message'>
        /// The description of the error.
        /// </param>
        public Error(ErrorType errorType, string message)
        {
            this.errorType = errorType;
            this.message = message;
            this.region = DomRegion.Empty;
        }
    }

    /// <summary>
    /// Represents a single file that was parsed.
    /// </summary>
    public interface IUnresolvedFile
    {
        /// <summary>
        /// Returns the full path of the file.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the time when the file was last written.
        /// </summary>
        DateTime? LastWriteTime { get; set; }

        /// <summary>
        /// Gets all top-level type definitions.
        /// </summary>
        IList<IUnresolvedTypeDefinition> TopLevelTypeDefinitions { get; }

        /// <summary>
        /// Gets all assembly attributes that are defined in this file.
        /// </summary>
        IList<IUnresolvedAttribute> AssemblyAttributes { get; }

        /// <summary>
        /// Gets all module attributes that are defined in this file.
        /// </summary>
        IList<IUnresolvedAttribute> ModuleAttributes { get; }

        /// <summary>
        /// Gets the top-level type defined at the specified location.
        /// Returns null if no type is defined at that location.
        /// </summary>
        IUnresolvedTypeDefinition GetTopLevelTypeDefinition(TextLocation location);

        /// <summary>
        /// Gets the type (potentially a nested type) defined at the specified location.
        /// Returns null if no type is defined at that location.
        /// </summary>
        IUnresolvedTypeDefinition GetInnermostTypeDefinition(TextLocation location);

        /// <summary>
        /// Gets the member defined at the specified location.
        /// Returns null if no member is defined at that location.
        /// </summary>
        IUnresolvedMember GetMember(TextLocation location);

        /// <summary>
        /// Gets the parser errors.
        /// </summary>
        IList<Error> Errors { get; }
    }

    public interface IAssemblyReference
    {
        /// <summary>
        /// Resolves this assembly.
        /// </summary>
        IAssembly Resolve(ITypeResolveContext context);
    }

    /// <summary>
    /// Represents an assembly consisting of source code (parsed files).
    /// </summary>
    public interface IProjectContent : IUnresolvedAssembly
    {
        /// <summary>
        /// Gets the path to the project file (e.g. .csproj).
        /// </summary>
        string ProjectFileName { get; }

        /// <summary>
        /// Gets a parsed file by its file name.
        /// </summary>
        IUnresolvedFile GetFile(string fileName);

        /// <summary>
        /// Gets the list of all files in the project content.
        /// </summary>
        IEnumerable<IUnresolvedFile> Files { get; }

        /// <summary>
        /// Gets the referenced assemblies.
        /// </summary>
        IEnumerable<IAssemblyReference> AssemblyReferences { get; }

        /// <summary>
        /// Gets the compiler settings object.
        /// The concrete type of the settings object depends on the programming language used to implement this project.
        /// </summary>
        object CompilerSettings { get; }

        /// <summary>
        /// Creates a new <see cref="ICompilation"/> that allows resolving within this project.
        /// </summary>
        /// <remarks>
        /// This method does not support <see cref="ProjectReference"/>s. When dealing with a solution
        /// containing multiple projects, consider using <see cref="ISolutionSnapshot.GetCompilation"/> instead.
        /// </remarks>
        ICompilation CreateCompilation();

        /// <summary>
        /// Creates a new <see cref="ICompilation"/> that allows resolving within this project.
        /// </summary>
        /// <param name="solutionSnapshot">The parent solution snapshot to use for the compilation.</param>
        /// <remarks>
        /// This method is intended to be called by ISolutionSnapshot implementations. Other code should
        /// call <see cref="ISolutionSnapshot.GetCompilation"/> instead.
        /// This method always creates a new compilation, even if the solution snapshot already contains
        /// one for this project.
        /// </remarks>
        ICompilation CreateCompilation(ISolutionSnapshot solutionSnapshot);

        /// <summary>
        /// Changes the assembly name of this project content.
        /// </summary>
        IProjectContent SetAssemblyName(string newAssemblyName);

        /// <summary>
        /// Changes the project file name of this project content.
        /// </summary>
        IProjectContent SetProjectFileName(string newProjectFileName);

        /// <summary>
        /// Changes the path to the assembly location (the output path where the project compiles to).
        /// </summary>
        IProjectContent SetLocation(string newLocation);

        /// <summary>
        /// Add assembly references to this project content.
        /// </summary>
        IProjectContent AddAssemblyReferences(IEnumerable<IAssemblyReference> references);

        /// <summary>
        /// Add assembly references to this project content.
        /// </summary>
        IProjectContent AddAssemblyReferences(params IAssemblyReference[] references);

        /// <summary>
        /// Removes assembly references from this project content.
        /// </summary>
        IProjectContent RemoveAssemblyReferences(IEnumerable<IAssemblyReference> references);

        /// <summary>
        /// Removes assembly references from this project content.
        /// </summary>
        IProjectContent RemoveAssemblyReferences(params IAssemblyReference[] references);

        /// <summary>
        /// Adds the specified files to the project content.
        /// If a file with the same name already exists, updated the existing file.
        /// </summary>
        /// <remarks>
        /// You can create an unresolved file by calling <c>ToTypeSystem()</c> on a syntax tree.
        /// </remarks>
        IProjectContent AddOrUpdateFiles(IEnumerable<IUnresolvedFile> newFiles);

        /// <summary>
        /// Adds the specified files to the project content.
        /// If a file with the same name already exists, this method updates the existing file.
        /// </summary>
        /// <remarks>
        /// You can create an unresolved file by calling <c>ToTypeSystem()</c> on a syntax tree.
        /// </remarks>
        IProjectContent AddOrUpdateFiles(params IUnresolvedFile[] newFiles);

        /// <summary>
        /// Removes the files with the specified names.
        /// </summary>
        IProjectContent RemoveFiles(IEnumerable<string> fileNames);

        /// <summary>
        /// Removes the files with the specified names.
        /// </summary>
        IProjectContent RemoveFiles(params string[] fileNames);

        /// <summary>
        /// Removes types and attributes from oldFile from the project, and adds those from newFile.
        /// </summary>
        [Obsolete("Use RemoveFiles()/AddOrUpdateFiles() instead")]
        IProjectContent UpdateProjectContent(IUnresolvedFile oldFile, IUnresolvedFile newFile);

        /// <summary>
        /// Removes types and attributes from oldFiles from the project, and adds those from newFiles.
        /// </summary>
        [Obsolete("Use RemoveFiles()/AddOrUpdateFiles() instead")]
        IProjectContent UpdateProjectContent(IEnumerable<IUnresolvedFile> oldFiles, IEnumerable<IUnresolvedFile> newFiles);

        /// <summary>
        /// Sets the compiler settings object.
        /// The concrete type of the settings object depends on the programming language used to implement this project.
        /// Using the incorrect type of settings object results in an <see cref="ArgumentException"/>.
        /// </summary>
        IProjectContent SetCompilerSettings(object compilerSettings);
    }
    /// <summary>
    /// Represents a snapshot of the whole solution (multiple compilations).
    /// </summary>
    public interface ISolutionSnapshot
    {
        /// <summary>
        /// Gets the project content with the specified file name.
        /// Returns null if no such project exists in the solution.
        /// </summary>
        /// <remarks>
        /// This method is used by the <see cref="ProjectReference"/> class.
        /// </remarks>
        IProjectContent GetProjectContent(string projectFileName);

        /// <summary>
        /// Gets the compilation for the specified project.
        /// The project must be a part of the solution (passed to the solution snapshot's constructor).
        /// </summary>
        ICompilation GetCompilation(IProjectContent project);
    }

    /// <summary>
    /// Allows caching values for a specific compilation.
    /// A CacheManager consists of a for shared instances (shared among all threads working with that resolve context).
    /// </summary>
    /// <remarks>This class is thread-safe</remarks>
    public sealed class CacheManager
    {
        readonly ConcurrentDictionary<object, object> sharedDict = new ConcurrentDictionary<object, object>(ReferenceComparer.Instance);
        // There used to be a thread-local dictionary here, but I removed it as it was causing memory
        // leaks in some use cases.

        public object GetShared(object key)
        {
            object value;
            sharedDict.TryGetValue(key, out value);
            return value;
        }

        public object GetOrAddShared(object key, Func<object, object> valueFactory)
        {
            return sharedDict.GetOrAdd(key, valueFactory);
        }

        public object GetOrAddShared(object key, object value)
        {
            return sharedDict.GetOrAdd(key, value);
        }

        public void SetShared(object key, object value)
        {
            sharedDict[key] = value;
        }
    }
    public interface ICompilation
    {
        /// <summary>
        /// Gets the current assembly.
        /// </summary>
        IAssembly MainAssembly { get; }

        /// <summary>
        /// Gets the type resolve context that specifies this compilation and no current assembly or entity.
        /// </summary>
        ITypeResolveContext TypeResolveContext { get; }

        /// <summary>
        /// Gets the list of all assemblies in the compilation.
        /// </summary>
        /// <remarks>
        /// This main assembly is the first entry in the list.
        /// </remarks>
        IList<IAssembly> Assemblies { get; }

        /// <summary>
        /// Gets the referenced assemblies.
        /// This list does not include the main assembly.
        /// </summary>
        IList<IAssembly> ReferencedAssemblies { get; }

        /// <summary>
        /// Gets the root namespace of this compilation.
        /// This is a merged version of the root namespaces of all assemblies.
        /// </summary>
        /// <remarks>
        /// This always is the namespace without a name - it's unrelated to the 'root namespace' project setting.
        /// </remarks>
        INamespace RootNamespace { get; }

        /// <summary>
        /// Gets the root namespace for a given extern alias.
        /// </summary>
        /// <remarks>
        /// If <paramref name="alias"/> is <c>null</c> or an empty string, this method
        /// returns the global root namespace.
        /// If no alias with the specified name exists, this method returns null.
        /// </remarks>
        INamespace GetNamespaceForExternAlias(string alias);

        IType FindType(KnownTypeCode typeCode);

        /// <summary>
        /// Gets the name comparer for the language being compiled.
        /// This is the string comparer used for the INamespace.GetTypeDefinition method.
        /// </summary>
        StringComparer NameComparer { get; }

        ISolutionSnapshot SolutionSnapshot { get; }

        CacheManager CacheManager { get; }
    }

    public interface ICompilationProvider
    {
        /// <summary>
        /// Gets the parent compilation.
        /// This property never returns null.
        /// </summary>
        ICompilation Compilation { get; }
    }

    /// <summary>
    /// Represents an unresolved assembly.
    /// </summary>
    public interface IUnresolvedAssembly : IAssemblyReference
    {
        /// <summary>
        /// Gets the assembly name (short name).
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// Gets the full assembly name (including public key token etc.)
        /// </summary>
        string FullAssemblyName { get; }

        /// <summary>
        /// Gets the path to the assembly location. 
        /// For projects it is the same as the output path.
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Gets the list of all assembly attributes in the project.
        /// </summary>
        IEnumerable<IUnresolvedAttribute> AssemblyAttributes { get; }

        /// <summary>
        /// Gets the list of all module attributes in the project.
        /// </summary>
        IEnumerable<IUnresolvedAttribute> ModuleAttributes { get; }

        /// <summary>
        /// Gets all non-nested types in the assembly.
        /// </summary>
        IEnumerable<IUnresolvedTypeDefinition> TopLevelTypeDefinitions { get; }
    }
    /// <summary>
    /// Holds the name of a top-level type.
    /// This struct cannot refer to nested classes.
    /// </summary>
    [Serializable]
    public struct TopLevelTypeName : IEquatable<TopLevelTypeName>
    {
        readonly string namespaceName;
        readonly string name;
        readonly int typeParameterCount;

        public TopLevelTypeName(string namespaceName, string name, int typeParameterCount = 0)
        {
            if (namespaceName == null)
                throw new ArgumentNullException("namespaceName");
            if (name == null)
                throw new ArgumentNullException("name");
            this.namespaceName = namespaceName;
            this.name = name;
            this.typeParameterCount = typeParameterCount;
        }

        public TopLevelTypeName(string reflectionName)
        {
            int pos = reflectionName.LastIndexOf('.');
            if (pos < 0)
            {
                namespaceName = string.Empty;
                name = reflectionName;
            }
            else
            {
                namespaceName = reflectionName.Substring(0, pos);
                name = reflectionName.Substring(pos + 1);
            }
            name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out typeParameterCount);
        }

        public string Namespace
        {
            get { return namespaceName; }
        }

        public string Name
        {
            get { return name; }
        }

        public int TypeParameterCount
        {
            get { return typeParameterCount; }
        }

        public string ReflectionName
        {
            get
            {
                StringBuilder b = new StringBuilder();
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    b.Append(namespaceName);
                    b.Append('.');
                }
                b.Append(name);
                if (typeParameterCount > 0)
                {
                    b.Append('`');
                    b.Append(typeParameterCount);
                }
                return b.ToString();
            }
        }

        public override string ToString()
        {
            return this.ReflectionName;
        }

        public override bool Equals(object obj)
        {
            return (obj is TopLevelTypeName) && Equals((TopLevelTypeName)obj);
        }

        public bool Equals(TopLevelTypeName other)
        {
            return this.namespaceName == other.namespaceName && this.name == other.name && this.typeParameterCount == other.typeParameterCount;
        }

        public override int GetHashCode()
        {
            return (name != null ? name.GetHashCode() : 0) ^ (namespaceName != null ? namespaceName.GetHashCode() : 0) ^ typeParameterCount;
        }

        public static bool operator ==(TopLevelTypeName lhs, TopLevelTypeName rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TopLevelTypeName lhs, TopLevelTypeName rhs)
        {
            return !lhs.Equals(rhs);
        }
    }


    /// <summary>
    /// Represents an assembly.
    /// </summary>
    public interface IAssembly : ICompilationProvider
    {
        /// <summary>
        /// Gets the original unresolved assembly.
        /// </summary>
        IUnresolvedAssembly UnresolvedAssembly { get; }

        /// <summary>
        /// Gets whether this assembly is the main assembly of the compilation.
        /// </summary>
        bool IsMainAssembly { get; }

        /// <summary>
        /// Gets the assembly name (short name).
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// Gets the full assembly name (including public key token etc.)
        /// </summary>
        string FullAssemblyName { get; }

        /// <summary>
        /// Gets the list of all assembly attributes in the project.
        /// </summary>
        IList<IAttribute> AssemblyAttributes { get; }

        /// <summary>
        /// Gets the list of all module attributes in the project.
        /// </summary>
        IList<IAttribute> ModuleAttributes { get; }

        /// <summary>
        /// Gets whether the internals of this assembly are visible in the specified assembly.
        /// </summary>
        bool InternalsVisibleTo(IAssembly assembly);

        /// <summary>
        /// Gets the root namespace for this assembly.
        /// </summary>
        /// <remarks>
        /// This always is the namespace without a name - it's unrelated to the 'root namespace' project setting.
        /// </remarks>
        INamespace RootNamespace { get; }

        /// <summary>
        /// Gets the type definition for a top-level type.
        /// </summary>
        /// <remarks>This method uses ordinal name comparison, not the compilation's name comparer.</remarks>
        ITypeDefinition GetTypeDefinition(TopLevelTypeName topLevelTypeName);

        /// <summary>
        /// Gets all non-nested types in the assembly.
        /// </summary>
        IEnumerable<ITypeDefinition> TopLevelTypeDefinitions { get; }
    }
    /// <summary>
    /// Represents a reference to a type.
    /// Must be resolved before it can be used as type.
    /// </summary>
    public interface ITypeReference
    {
        // Keep this interface simple: I decided against having GetMethods/GetEvents etc. here,
        // so that the Resolve step is never hidden from the consumer.

        // I decided against implementing IFreezable here: IUnresolvedTypeDefinition can be used as ITypeReference,
        // but when freezing the reference, one wouldn't expect the definition to freeze.

        /// <summary>
        /// Resolves this type reference.
        /// </summary>
        /// <param name="context">
        /// Context to use for resolving this type reference.
        /// Which kind of context is required depends on the which kind of type reference this is;
        /// please consult the documentation of the method that was used to create this type reference,
        /// or that of the class implementing this method.
        /// </param>
        /// <returns>
        /// Returns the resolved type.
        /// In case of an error, returns an unknown type (<see cref="TypeKind.Unknown"/>).
        /// Never returns null.
        /// </returns>
        IType Resolve(ITypeResolveContext context);
    }

    public interface ITypeResolveContext : ICompilationProvider
    {
        /// <summary>
        /// Gets the current assembly.
        /// This property may return null if this context does not specify any assembly.
        /// </summary>
        IAssembly CurrentAssembly { get; }

        /// <summary>
        /// Gets the current type definition.
        /// </summary>
        ITypeDefinition CurrentTypeDefinition { get; }

        /// <summary>
        /// Gets the current member.
        /// </summary>
        IMember CurrentMember { get; }

        ITypeResolveContext WithCurrentTypeDefinition(ITypeDefinition typeDefinition);
        ITypeResolveContext WithCurrentMember(IMember member);
    }
    /// <summary>
    /// Represents an unresolved class, enum, interface, struct, delegate or VB module.
    /// For partial classes, an unresolved type definition represents only a single part.
    /// </summary>
    public interface IUnresolvedTypeDefinition : ITypeReference, IUnresolvedEntity
    {
        TypeKind Kind { get; }

        FullTypeName FullTypeName { get; }
        IList<ITypeReference> BaseTypes { get; }
        IList<IUnresolvedTypeParameter> TypeParameters { get; }

        IList<IUnresolvedTypeDefinition> NestedTypes { get; }
        IList<IUnresolvedMember> Members { get; }

        IEnumerable<IUnresolvedMethod> Methods { get; }
        IEnumerable<IUnresolvedProperty> Properties { get; }
        IEnumerable<IUnresolvedField> Fields { get; }
        IEnumerable<IUnresolvedEvent> Events { get; }

        /// <summary>
        /// Gets whether the type definition contains extension methods.
        /// Returns null when the type definition needs to be resolved in order to determine whether
        /// methods are extension methods.
        /// </summary>
        bool? HasExtensionMethods { get; }

        /// <summary>
        /// Gets whether the partial modifier is set on this part of the type definition.
        /// </summary>
        bool IsPartial { get; }

        /// <summary>
        /// Gets whether this unresolved type definition causes the addition of a default constructor
        /// if no other constructor is present.
        /// </summary>
        bool AddDefaultConstructorIfRequired { get; }

        /// <summary>
        /// Looks up the resolved type definition from the <paramref name="context"/> corresponding to this unresolved
        /// type definition.
        /// </summary>
        /// <param name="context">
        /// Context for looking up the type. The context must specify the current assembly.
        /// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
        /// </param>
        /// <returns>
        /// Returns the resolved type definition.
        /// In case of an error, returns an <see cref="Implementation.UnknownType"/> instance.
        /// Never returns null.
        /// </returns>
        new IType Resolve(ITypeResolveContext context);

        /// <summary>
        /// This method is used to add language-specific elements like the C# UsingScope
        /// to the type resolve context.
        /// </summary>
        /// <param name="parentContext">The parent context (e.g. the parent assembly),
        /// including the parent type definition for inner classes.</param>
        /// <returns>
        /// The parent context, modified to include language-specific elements (e.g. using scope)
        /// associated with this type definition.
        /// </returns>
        /// <remarks>
        /// Use <c>unresolvedTypeDef.CreateResolveContext(parentContext).WithTypeDefinition(typeDef)</c> to
        /// create the context for use within the type definition.
        /// </remarks>
        ITypeResolveContext CreateResolveContext(ITypeResolveContext parentContext);
    }

    /// <summary>
    /// Represents a class, enum, interface, struct, delegate or VB module.
    /// For partial classes, this represents the whole class.
    /// </summary>
    public interface ITypeDefinition : IType, IEntity
    {
        /// <summary>
        /// Returns all parts that contribute to this type definition.
        /// Non-partial classes have a single part that represents the whole class.
        /// </summary>
        IList<IUnresolvedTypeDefinition> Parts { get; }

        IList<ITypeParameter> TypeParameters { get; }

        IList<ITypeDefinition> NestedTypes { get; }
        IList<IMember> Members { get; }

        IEnumerable<IField> Fields { get; }
        IEnumerable<IMethod> Methods { get; }
        IEnumerable<IProperty> Properties { get; }
        IEnumerable<IEvent> Events { get; }

        /// <summary>
        /// Gets the known type code for this type definition.
        /// </summary>
        KnownTypeCode KnownTypeCode { get; }

        /// <summary>
        /// For enums: returns the underlying primitive type.
        /// For all other types: returns <see cref="SpecialType.UnknownType"/>.
        /// </summary>
        IType EnumUnderlyingType { get; }

        /// <summary>
        /// Gets the full name of this type.
        /// </summary>
        FullTypeName FullTypeName { get; }

        /// <summary>
        /// Gets/Sets the declaring type (incl. type arguments, if any).
        /// This property will return null for top-level types.
        /// </summary>
        new IType DeclaringType { get; } // solves ambiguity between IType.DeclaringType and IEntity.DeclaringType

        /// <summary>
        /// Gets whether this type contains extension methods.
        /// </summary>
        /// <remarks>This property is used to speed up the search for extension methods.</remarks>
        bool HasExtensionMethods { get; }

        /// <summary>
        /// Gets whether this type definition is made up of one or more partial classes.
        /// </summary>
        bool IsPartial { get; }

        /// <summary>
        /// Determines how this type is implementing the specified interface member.
        /// </summary>
        /// <returns>
        /// The method on this type that implements the interface member;
        /// or null if the type does not implement the interface.
        /// </returns>
        IMember GetInterfaceImplementation(IMember interfaceMember);

        /// <summary>
        /// Determines how this type is implementing the specified interface members.
        /// </summary>
        /// <returns>
        /// For each interface member, this method returns the class member 
        /// that implements the interface member.
        /// For interface members that are missing an implementation, the
        /// result collection will contain a null element.
        /// </returns>
        IList<IMember> GetInterfaceImplementation(IList<IMember> interfaceMembers);
    }

    /// <summary>
    /// This interface represents a resolved type in the type system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A type is potentially
    /// - a type definition (<see cref="ITypeDefinition"/>, i.e. a class, struct, interface, delegate, or built-in primitive type)
    /// - a parameterized type (<see cref="ParameterizedType"/>, e.g. List&lt;int>)
    /// - a type parameter (<see cref="ITypeParameter"/>, e.g. T)
    /// - an array (<see cref="ArrayType"/>)
    /// - a pointer (<see cref="PointerType"/>)
    /// - a managed reference (<see cref="ByReferenceType"/>)
    /// - one of the special types (<see cref="SpecialType.UnknownType"/>, <see cref="SpecialType.NullType"/>,
    ///      <see cref="SpecialType.Dynamic"/>, <see cref="SpecialType.UnboundTypeArgument"/>)
    /// 
    /// The <see cref="IType.Kind"/> property can be used to switch on the kind of a type.
    /// </para>
    /// <para>
    /// IType uses the null object pattern: <see cref="SpecialType.UnknownType"/> serves as the null object.
    /// Methods or properties returning IType never return null unless documented otherwise.
    /// </para>
    /// <para>
    /// Types should be compared for equality using the <see cref="IEquatable{IType}.Equals(IType)"/> method.
    /// Identical types do not necessarily use the same object reference.
    /// </para>
    /// </remarks>
    public interface IType : INamedElement, IEquatable<IType>
    {
        /// <summary>
        /// Gets the type kind.
        /// </summary>
        TypeKind Kind { get; }

        /// <summary>
        /// Gets whether the type is a reference type or value type.
        /// </summary>
        /// <returns>
        /// true, if the type is a reference type.
        /// false, if the type is a value type.
        /// null, if the type is not known (e.g. unconstrained generic type parameter or type not found)
        /// </returns>
        bool? IsReferenceType { get; }

        /// <summary>
        /// Gets the underlying type definition.
        /// Can return null for types which do not have a type definition (for example arrays, pointers, type parameters).
        /// </summary>
        ITypeDefinition GetDefinition();

        /// <summary>
        /// Gets the parent type, if this is a nested type.
        /// Returns null for top-level types.
        /// </summary>
        IType DeclaringType { get; }

        /// <summary>
        /// Gets the number of type parameters.
        /// </summary>
        int TypeParameterCount { get; }

        /// <summary>
        /// Gets the type arguments passed to this type.
        /// If this type is a generic type definition that is not parameterized, this property returns the type parameters,
        /// as if the type was parameterized with its own type arguments (<c>class C&lt;T&gt; { C&lt;T&gt; field; }</c>).
        /// 
        /// NOTE: The type will change to IReadOnlyList&lt;IType&gt; in future versions.
        /// </summary>
        IList<IType> TypeArguments { get; }

        /// <summary>
        /// If true the type represents an instance of a generic type.
        /// </summary>
        bool IsParameterized { get; }

        /// <summary>
        /// Calls ITypeVisitor.Visit for this type.
        /// </summary>
        /// <returns>The return value of the ITypeVisitor.Visit call</returns>
        IType AcceptVisitor(TypeVisitor visitor);

        /// <summary>
        /// Calls ITypeVisitor.Visit for all children of this type, and reconstructs this type with the children based
        /// on the return values of the visit calls.
        /// </summary>
        /// <returns>A copy of this type, with all children replaced by the return value of the corresponding visitor call.
        /// If the visitor returned the original types for all children (or if there are no children), returns <c>this</c>.
        /// </returns>
        IType VisitChildren(TypeVisitor visitor);

        /// <summary>
        /// Gets the direct base types.
        /// </summary>
        /// <returns>Returns the direct base types including interfaces</returns>
        IEnumerable<IType> DirectBaseTypes { get; }

        /// <summary>
        /// Creates a type reference that can be used to look up a type equivalent to this type in another compilation.
        /// </summary>
        /// <remarks>
        /// If this type contains open generics, the resulting type reference will need to be looked up in an appropriate generic context.
        /// Otherwise, the main resolve context of a compilation is sufficient.
        /// </remarks>
        ITypeReference ToTypeReference();

        /// <summary>
        /// Gets a type visitor that performs the substitution of class type parameters with the type arguments
        /// of this parameterized type.
        /// Returns TypeParameterSubstitution.Identity if the type is not parametrized.
        /// </summary>
        TypeParameterSubstitution GetSubstitution();

        /// <summary>
        /// Gets a type visitor that performs the substitution of class type parameters with the type arguments
        /// of this parameterized type,
        /// and also substitutes method type parameters with the specified method type arguments.
        /// Returns TypeParameterSubstitution.Identity if the type is not parametrized.
        /// </summary>
        TypeParameterSubstitution GetSubstitution(IList<IType> methodTypeArguments);


        /// <summary>
        /// Gets inner classes (including inherited inner classes).
        /// </summary>
        /// <param name="filter">The filter used to select which types to return.
        /// The filter is tested on the original type definitions (before parameterization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// <para>
        /// If the nested type is generic, this method will return a parameterized type,
        /// where the additional type parameters are set to <see cref="SpecialType.UnboundTypeArgument"/>.
        /// </para>
        /// <para>
        /// Type parameters belonging to the outer class will have the value copied from the outer type
        /// if it is a parameterized type. Otherwise, those existing type parameters will be self-parameterized,
        /// and thus 'leaked' to the caller in the same way the GetMembers() method does not specialize members
        /// from an <see cref="ITypeDefinition"/> and 'leaks' type parameters in member signatures.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// class Base&lt;T> {
        /// 	class Nested&lt;X> {}
        /// }
        /// class Derived&lt;A, B> : Base&lt;B> {}
        /// 
        /// Derived[string,int].GetNestedTypes() = { Base`1+Nested`1[int, unbound] }
        /// Derived.GetNestedTypes() = { Base`1+Nested`1[`1, unbound] }
        /// Base[`1].GetNestedTypes() = { Base`1+Nested`1[`1, unbound] }
        /// Base.GetNestedTypes() = { Base`1+Nested`1[`0, unbound] }
        /// </code>
        /// </example>
        IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None);

        // Note that we cannot 'leak' the additional type parameter as we leak the normal type parameters, because
        // the index might collide. For example,
        //   class Base<T> { class Nested<X> {} }
        //   class Derived<A, B> : Base<B> { }
        // 
        // Derived<string, int>.GetNestedTypes() = Base+Nested<int, UnboundTypeArgument>
        // Derived.GetNestedTypes() = Base+Nested<`1, >
        //  Here `1 refers to B, and there's no way to return X as it would collide with B.

        /// <summary>
        /// Gets inner classes (including inherited inner classes)
        /// that have <c>typeArguments.Count</c> additional type parameters.
        /// </summary>
        /// <param name="typeArguments">The type arguments passed to the inner class</param>
        /// <param name="filter">The filter used to select which types to return.
        /// The filter is tested on the original type definitions (before parameterization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// Type parameters belonging to the outer class will have the value copied from the outer type
        /// if it is a parameterized type. Otherwise, those existing type parameters will be self-parameterized,
        /// and thus 'leaked' to the caller in the same way the GetMembers() method does not specialize members
        /// from an <see cref="ITypeDefinition"/> and 'leaks' type parameters in member signatures.
        /// </remarks>
        IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all instance constructors for this type.
        /// </summary>
        /// <param name="filter">The filter used to select which constructors to return.
        /// The filter is tested on the original method definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// <para>The result does not include static constructors.
        /// Constructors in base classes are not returned by default, as GetMemberOptions.IgnoreInheritedMembers is the default value.</para>
        /// <para>
        /// For methods on parameterized types, type substitution will be performed on the method signature,
        /// and the appropriate <see cref="Implementation.SpecializedMethod"/> will be returned.
        /// </para>
        /// </remarks>
        IEnumerable<IMethod> GetConstructors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers);

        /// <summary>
        /// Gets all methods that can be called on this type.
        /// </summary>
        /// <param name="filter">The filter used to select which methods to return.
        /// The filter is tested on the original method definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// <para>
        /// The result does not include constructors or accessors.
        /// </para>
        /// <para>
        /// For methods on parameterized types, type substitution will be performed on the method signature,
        /// and the appropriate <see cref="Implementation.SpecializedMethod"/> will be returned.
        /// </para>
        /// <para>
        /// If the method being returned is generic, and this type is a parameterized type where the type
        /// arguments involve another method's type parameters, the resulting specialized signature
        /// will be ambiguous as to which method a type parameter belongs to.
        /// For example, "List[[``0]].GetMethods()" will return "ConvertAll(Converter`2[[``0, ``0]])".
        /// 
        /// If possible, use the other GetMethods() overload to supply type arguments to the method,
        /// so that both class and method type parameter can be substituted at the same time, so that
        /// the ambiguity can be avoided.
        /// </para>
        /// </remarks>
        IEnumerable<IMethod> GetMethods(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all generic methods that can be called on this type with the specified type arguments.
        /// </summary>
        /// <param name="typeArguments">The type arguments used for the method call.</param>
        /// <param name="filter">The filter used to select which methods to return.
        /// The filter is tested on the original method definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// <para>The result does not include constructors or accessors.</para>
        /// <para>
        /// Type substitution will be performed on the method signature, creating a <see cref="Implementation.SpecializedMethod"/>
        /// with the specified type arguments.
        /// </para>
        /// <para>
        /// When the list of type arguments is empty, this method acts like the GetMethods() overload without
        /// the type arguments parameter - that is, it also returns generic methods,
        /// and the other overload's remarks about ambiguous signatures apply here as well.
        /// </para>
        /// </remarks>
        IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all properties that can be called on this type.
        /// </summary>
        /// <param name="filter">The filter used to select which properties to return.
        /// The filter is tested on the original property definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// For properties on parameterized types, type substitution will be performed on the property signature,
        /// and the appropriate <see cref="Implementation.SpecializedProperty"/> will be returned.
        /// </remarks>
        IEnumerable<IProperty> GetProperties(Predicate<IUnresolvedProperty> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all fields that can be accessed on this type.
        /// </summary>
        /// <param name="filter">The filter used to select which constructors to return.
        /// The filter is tested on the original field definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// For fields on parameterized types, type substitution will be performed on the field's return type,
        /// and the appropriate <see cref="Implementation.SpecializedField"/> will be returned.
        /// </remarks>
        IEnumerable<IField> GetFields(Predicate<IUnresolvedField> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all events that can be accessed on this type.
        /// </summary>
        /// <param name="filter">The filter used to select which events to return.
        /// The filter is tested on the original event definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// For fields on parameterized types, type substitution will be performed on the event's return type,
        /// and the appropriate <see cref="Implementation.SpecializedEvent"/> will be returned.
        /// </remarks>
        IEnumerable<IEvent> GetEvents(Predicate<IUnresolvedEvent> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all members that can be called on this type.
        /// </summary>
        /// <param name="filter">The filter used to select which members to return.
        /// The filter is tested on the original member definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// <para>
        /// The resulting list is the union of GetFields(), GetProperties(), GetMethods() and GetEvents().
        /// It does not include constructors.
        /// For parameterized types, type substitution will be performed.
        /// </para>
        /// <para>
        /// For generic methods, the remarks about ambiguous signatures from the
        /// <see cref="GetMethods(Predicate{IUnresolvedMethod}, GetMemberOptions)"/> method apply here as well.
        /// </para>
        /// </remarks>
        IEnumerable<IMember> GetMembers(Predicate<IUnresolvedMember> filter = null, GetMemberOptions options = GetMemberOptions.None);

        /// <summary>
        /// Gets all accessors belonging to properties or events on this type.
        /// </summary>
        /// <param name="filter">The filter used to select which members to return.
        /// The filter is tested on the original member definitions (before specialization).</param>
        /// <param name="options">Specified additional options for the GetMembers() operation.</param>
        /// <remarks>
        /// Accessors are not returned by GetMembers() or GetMethods().
        /// </remarks>
        IEnumerable<IMethod> GetAccessors(Predicate<IUnresolvedMethod> filter = null, GetMemberOptions options = GetMemberOptions.None);
    }

    /// <summary>
    /// .
    /// </summary>
    public enum TypeKind : byte
    {
        /// <summary>Language-specific type that is not part of NRefactory.TypeSystem itself.</summary>
        Other,

        /// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is a class.</summary>
        Class,
        /// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is an interface.</summary>
        Interface,
        /// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is a struct.</summary>
        Struct,
        /// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is a delegate.</summary>
        /// <remarks><c>System.Delegate</c> itself is TypeKind.Class</remarks>
        Delegate,
        /// <summary>A <see cref="ITypeDefinition"/> that is an enum.</summary>
        /// <remarks><c>System.Enum</c> itself is TypeKind.Class</remarks>
        Enum,
        /// <summary>A <see cref="ITypeDefinition"/> that is a module (VB).</summary>
        Module,

        /// <summary>The <c>System.Void</c> type.</summary>
        /// <see cref="KnownTypeReference.Void"/>
        Void,

        /// <see cref="SpecialType.UnknownType"/>
        Unknown,
        /// <summary>The type of the null literal.</summary>
        /// <see cref="SpecialType.NullType"/>
        Null,
        /// <summary>Type representing the C# 'dynamic' type.</summary>
        /// <see cref="SpecialType.Dynamic"/>
        Dynamic,
        /// <summary>Represents missing type arguments in partially parameterized types.</summary>
        /// <see cref="SpecialType.UnboundTypeArgument"/>
        /// <see cref="IType.GetNestedTypes(Predicate{ITypeDefinition}, GetMemberOptions)"/>
        UnboundTypeArgument,

        /// <summary>The type is a type parameter.</summary>
        /// <see cref="ITypeParameter"/>
        TypeParameter,

        /// <summary>An array type</summary>
        /// <see cref="ArrayType"/>
        Array,
        /// <summary>A pointer type</summary>
        /// <see cref="PointerType"/>
        Pointer,
        /// <summary>A managed reference type</summary>
        /// <see cref="ByReferenceType"/>
        ByReference,
        /// <summary>An anonymous type</summary>
        /// <see cref="AnonymousType"/>
        Anonymous,

        /// <summary>Intersection of several types</summary>
        /// <see cref="IntersectionType"/>
        Intersection,
        /// <see cref="SpecialType.ArgList"/>
        ArgList,
    }
    public interface INamedElement
    {
        /// <summary>
        /// Gets the fully qualified name of the class the return type is pointing to.
        /// </summary>
        /// <returns>
        /// "System.Int32[]" for int[]<br/>
        /// "System.Collections.Generic.List" for List&lt;string&gt;
        /// "System.Environment.SpecialFolder" for Environment.SpecialFolder
        /// </returns>
        string FullName { get; }

        /// <summary>
        /// Gets the short name of the class the return type is pointing to.
        /// </summary>
        /// <returns>
        /// "Int32[]" for int[]<br/>
        /// "List" for List&lt;string&gt;
        /// "SpecialFolder" for Environment.SpecialFolder
        /// </returns>
        string Name { get; }

        /// <summary>
        /// Gets the full reflection name of the element.
        /// </summary>
        /// <remarks>
        /// For types, the reflection name can be parsed back into a ITypeReference by using
        /// <see cref="ReflectionHelper.ParseReflectionName(string)"/>.
        /// </remarks>
        /// <returns>
        /// "System.Int32[]" for int[]<br/>
        /// "System.Int32[][,]" for C# int[,][]<br/>
        /// "System.Collections.Generic.List`1[[System.String]]" for List&lt;string&gt;
        /// "System.Environment+SpecialFolder" for Environment.SpecialFolder
        /// </returns>
        string ReflectionName { get; }

        /// <summary>
        /// Gets the full name of the namespace containing this entity.
        /// </summary>
        string Namespace { get; }
    }

    public enum SymbolKind : byte
    {
        None,
        /// <seealso cref="ITypeDefinition"/>
        TypeDefinition,
        /// <seealso cref="IField"/>
        Field,
        /// <summary>
        /// The symbol is a property, but not an indexer.
        /// </summary>
        /// <seealso cref="IProperty"/>
        Property,
        /// <summary>
        /// The symbol is an indexer, not a regular property.
        /// </summary>
        /// <seealso cref="IProperty"/>
        Indexer,
        /// <seealso cref="IEvent"/>
        Event,
        /// <summary>
        /// The symbol is a method which is not an operator/constructor/destructor or accessor.
        /// </summary>
        /// <seealso cref="IMethod"/>
        Method,
        /// <summary>
        /// The symbol is a user-defined operator.
        /// </summary>
        /// <seealso cref="IMethod"/>
        Operator,
        /// <seealso cref="IMethod"/>
        Constructor,
        /// <seealso cref="IMethod"/>
        Destructor,
        /// <summary>
        /// The accessor method for a property getter/setter or event add/remove.
        /// </summary>
        /// <seealso cref="IMethod"/>
        Accessor,
        /// <seealso cref="INamespace"/>
        Namespace,
        /// <summary>
        /// The symbol is a variable, but not a parameter.
        /// </summary>
        /// <seealso cref="IVariable"/>
        Variable,
        /// <seealso cref="IParameter"/>
        Parameter,
        /// <seealso cref="ITypeParameter"/>
        TypeParameter,
    }

    /// <summary>
    /// Interface for type system symbols.
    /// </summary>
    public interface ISymbol
    {
        /// <summary>
        /// This property returns an enum specifying which kind of symbol this is
        /// (which derived interfaces of ISymbol are implemented)
        /// </summary>
        SymbolKind SymbolKind { get; }

        /// <summary>
        /// Gets the short name of the symbol.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Creates a symbol reference that can be used to rediscover this symbol in another compilation.
        /// </summary>
        ISymbolReference ToReference();
    }

    /// <summary>
    /// Represents an unresolved entity.
    /// </summary>
    public interface IUnresolvedEntity : INamedElement, IHasAccessibility
    {
        /// <summary>
        /// Gets the entity type.
        /// </summary>
        SymbolKind SymbolKind { get; }

        /// <summary>
        /// Gets the complete entity region (including header+body)
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Gets the entity body region.
        /// </summary>
        DomRegion BodyRegion { get; }

        /// <summary>
        /// Gets the declaring class.
        /// For members, this is the class that contains the member.
        /// For nested classes, this is the outer class. For top-level entities, this property returns null.
        /// </summary>
        IUnresolvedTypeDefinition DeclaringTypeDefinition { get; }

        /// <summary>
        /// Gets the parsed file in which this entity is defined.
        /// Returns null if this entity wasn't parsed from source code (e.g. loaded from a .dll with CecilLoader).
        /// </summary>
        IUnresolvedFile UnresolvedFile { get; }

        /// <summary>
        /// Gets the attributes on this entity.
        /// </summary>
        IList<IUnresolvedAttribute> Attributes { get; }

        /// <summary>
        /// Gets whether this entity is static.
        /// Returns true if either the 'static' or the 'const' modifier is set.
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Returns whether this entity is abstract.
        /// </summary>
        /// <remarks>Static classes also count as abstract classes.</remarks>
        bool IsAbstract { get; }

        /// <summary>
        /// Returns whether this entity is sealed.
        /// </summary>
        /// <remarks>Static classes also count as sealed classes.</remarks>
        bool IsSealed { get; }

        /// <summary>
        /// Gets whether this member is declared to be shadowing another member with the same name.
        /// </summary>
        bool IsShadowing { get; }

        /// <summary>
        /// Gets whether this member is generated by a macro/compiler feature.
        /// </summary>
        bool IsSynthetic { get; }
    }

    /// <summary>
    /// Represents a resolved entity.
    /// </summary>
    public interface IEntity : ISymbol, ICompilationProvider, INamedElement, IHasAccessibility
    {
        /// <summary>
        /// Gets the entity type.
        /// </summary>
        [Obsolete("Use the SymbolKind property instead.")]
        EntityType EntityType { get; }

        /// <summary>
        /// Gets the short name of the entity.
        /// </summary>
        new string Name { get; }

        /// <summary>
        /// Gets the complete entity region (including header+body)
        /// </summary>
        DomRegion Region { get; }

        /// <summary>
        /// Gets the entity body region.
        /// </summary>
        DomRegion BodyRegion { get; }

        /// <summary>
        /// Gets the declaring class.
        /// For members, this is the class that contains the member.
        /// For nested classes, this is the outer class. For top-level entities, this property returns null.
        /// </summary>
        ITypeDefinition DeclaringTypeDefinition { get; }

        /// <summary>
        /// Gets/Sets the declaring type (incl. type arguments, if any).
        /// This property will return null for top-level entities.
        /// If this is not a specialized member, the value returned is equal to <see cref="DeclaringTypeDefinition"/>.
        /// </summary>
        IType DeclaringType { get; }

        /// <summary>
        /// The assembly in which this entity is defined.
        /// This property never returns null.
        /// </summary>
        IAssembly ParentAssembly { get; }

        /// <summary>
        /// Gets the attributes on this entity.
        /// </summary>
        IList<IAttribute> Attributes { get; }

        /// <summary>
        /// Gets the documentation for this entity.
        /// </summary>
        DocumentationComment Documentation { get; }

        /// <summary>
        /// Gets whether this entity is static.
        /// Returns true if either the 'static' or the 'const' modifier is set.
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Returns whether this entity is abstract.
        /// </summary>
        /// <remarks>Static classes also count as abstract classes.</remarks>
        bool IsAbstract { get; }

        /// <summary>
        /// Returns whether this entity is sealed.
        /// </summary>
        /// <remarks>Static classes also count as sealed classes.</remarks>
        bool IsSealed { get; }

        /// <summary>
        /// Gets whether this member is declared to be shadowing another member with the same name.
        /// (C# 'new' keyword)
        /// </summary>
        bool IsShadowing { get; }

        /// <summary>
        /// Gets whether this member is generated by a macro/compiler feature.
        /// </summary>
        bool IsSynthetic { get; }
    }
}


namespace ICSharpCode.NRefactory.Completion
{
    using ICSharpCode.NRefactory.TypeSystem;

    /// <summary>
    /// Provides intellisense information for a collection of parametrized members.
    /// </summary>
    public interface IParameterDataProvider
    {
        /// <summary>
        /// Gets the overload count.
        /// </summary>
        int Count
        {
            get;
        }

        /// <summary>
        /// Gets the start offset of the parameter expression node.
        /// </summary>
        int StartOffset
        {
            get;
        }

        /// <summary>
        /// Returns the markup to use to represent the specified method overload
        /// in the parameter information window.
        /// </summary>
        string GetHeading(int overload, string[] parameterDescription, int currentParameter);

        /// <summary>
        /// Returns the markup for the description to use to represent the specified method overload
        /// in the parameter information window.
        /// </summary>
        string GetDescription(int overload, int currentParameter);

        /// <summary>
        /// Returns the text to use to represent the specified parameter
        /// </summary>
        string GetParameterDescription(int overload, int paramIndex);

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        string GetParameterName(int overload, int currentParameter);

        /// <summary>
        /// Returns the number of parameters of the specified method
        /// </summary>
        int GetParameterCount(int overload);

        /// <summary>
        /// Used for the params lists. (for example "params" in c#).
        /// </summary>
        bool AllowParameterList(int overload);
    }

    public enum DisplayFlags
    {
        None = 0,
        Hidden = 1,
        Obsolete = 2,
        DescriptionHasMarkup = 4,
        NamedArgument = 8,
        IsImportCompletion = 16,
        MarkedBold = 32
    }
    public abstract class CompletionCategory : IComparable<CompletionCategory>
    {
        public string DisplayText { get; set; }

        public string Icon { get; set; }

        protected CompletionCategory()
        {
        }

        protected CompletionCategory(string displayText, string icon)
        {
            this.DisplayText = displayText;
            this.Icon = icon;
        }

        public abstract int CompareTo(CompletionCategory other);
    }
    public interface ICompletionData
    {
        CompletionCategory CompletionCategory { get; set; }

        string DisplayText { get; set; }

        string Description { get; set; }

        string CompletionText { get; set; }

        DisplayFlags DisplayFlags { get; set; }

        bool HasOverloads
        {
            get;
        }

        IEnumerable<ICompletionData> OverloadedData
        {
            get;
        }

        void AddOverload(ICompletionData data);
    }
    public interface ICompletionDataFactory
    {
        ICompletionData CreateEntityCompletionData(IEntity entity);
        ICompletionData CreateEntityCompletionData(IEntity entity, string text);

        ICompletionData CreateTypeCompletionData(IType type, bool showFullName, bool isInAttributeContext, bool addForTypeCreation);

        /// <summary>
        /// Creates the member completion data. 
        /// Form: Type.Member
        /// Used for generating enum members Foo.A, Foo.B where the enum 'Foo' is valid.
        /// </summary>
        ICompletionData CreateMemberCompletionData(IType type, IEntity member);

        /// <summary>
        /// Creates a generic completion data.
        /// </summary>
        /// <param name='title'>
        /// The title of the completion data
        /// </param>
        /// <param name='description'>
        /// The description of the literal.
        /// </param>
        /// <param name='insertText'>
        /// The insert text. If null, title is taken.
        /// </param>
        ICompletionData CreateLiteralCompletionData(string title, string description = null, string insertText = null);

        ICompletionData CreateNamespaceCompletionData(INamespace name);

        ICompletionData CreateVariableCompletionData(IVariable variable);

        ICompletionData CreateVariableCompletionData(ITypeParameter parameter);

        ICompletionData CreateEventCreationCompletionData(string delegateMethodName, IType delegateType, IEvent evt, string parameterDefinition, IUnresolvedMember currentMember, IUnresolvedTypeDefinition currentType);

        ICompletionData CreateNewOverrideCompletionData(int declarationBegin, IUnresolvedTypeDefinition type, IMember m);
        ICompletionData CreateNewPartialCompletionData(int declarationBegin, IUnresolvedTypeDefinition type, IUnresolvedMember m);

        IEnumerable<ICompletionData> CreateCodeTemplateCompletionData();

        IEnumerable<ICompletionData> CreatePreProcessorDefinesCompletionData();

        /// <summary>
        /// Creates a completion data that adds the required using for the created type.
        /// </summary>
        /// <param name="type">The type to import</param>
        /// <param name="useFullName">If set to true the full name of the type needs to be used.</param>
        /// <param name="addForTypeCreation">If true the completion data is used in 'new' context.</param>
        ICompletionData CreateImportCompletionData(IType type, bool useFullName, bool addForTypeCreation);

        ICompletionData CreateFormatItemCompletionData(string format, string description, object example);

        ICompletionData CreateXmlDocCompletionData(string tag, string description = null, string tagInsertionText = null);

    }
}
namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
    /// <summary>
    /// Provides helper methods for implementing GetMembers() on IType-implementations.
    /// Note: GetMembersHelper will recursively call back into IType.GetMembers(), but only with
    /// both GetMemberOptions.IgnoreInheritedMembers and GetMemberOptions.ReturnMemberDefinitions set,
    /// and only the 'simple' overloads (not taking type arguments).
    /// 
    /// Ensure that your IType implementation does not use the GetMembersHelper if both flags are set,
    /// otherwise you'll get a StackOverflowException!
    /// </summary>
    static class GetMembersHelper
    {
        #region GetNestedTypes
        public static IEnumerable<IType> GetNestedTypes(IType type, Predicate<ITypeDefinition> filter, GetMemberOptions options)
        {
            return GetNestedTypes(type, null, filter, options);
        }

        public static IEnumerable<IType> GetNestedTypes(IType type, IList<IType> nestedTypeArguments, Predicate<ITypeDefinition> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetNestedTypesImpl(type, nestedTypeArguments, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetNestedTypesImpl(t, nestedTypeArguments, filter, options));
            }
        }

        static IEnumerable<IType> GetNestedTypesImpl(IType outerType, IList<IType> nestedTypeArguments, Predicate<ITypeDefinition> filter, GetMemberOptions options)
        {
            ITypeDefinition outerTypeDef = outerType.GetDefinition();
            if (outerTypeDef == null)
                yield break;

            int outerTypeParameterCount = outerTypeDef.TypeParameterCount;
            ParameterizedType pt = outerType as ParameterizedType;
            foreach (ITypeDefinition nestedType in outerTypeDef.NestedTypes)
            {
                int totalTypeParameterCount = nestedType.TypeParameterCount;
                if (nestedTypeArguments != null)
                {
                    if (totalTypeParameterCount - outerTypeParameterCount != nestedTypeArguments.Count)
                        continue;
                }
                if (!(filter == null || filter(nestedType)))
                    continue;

                if (totalTypeParameterCount == 0 || (options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
                {
                    yield return nestedType;
                }
                else
                {
                    // We need to parameterize the nested type
                    IType[] newTypeArguments = new IType[totalTypeParameterCount];
                    for (int i = 0; i < outerTypeParameterCount; i++)
                    {
                        newTypeArguments[i] = pt != null ? pt.GetTypeArgument(i) : outerTypeDef.TypeParameters[i];
                    }
                    for (int i = outerTypeParameterCount; i < totalTypeParameterCount; i++)
                    {
                        if (nestedTypeArguments != null)
                            newTypeArguments[i] = nestedTypeArguments[i - outerTypeParameterCount];
                        else
                            newTypeArguments[i] = SpecialType.UnboundTypeArgument;
                    }
                    yield return new ParameterizedType(nestedType, newTypeArguments);
                }
            }
        }
        #endregion

        #region GetMethods
        public static IEnumerable<IMethod> GetMethods(IType type, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            return GetMethods(type, null, filter, options);
        }

        public static IEnumerable<IMethod> GetMethods(IType type, IList<IType> typeArguments, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            if (typeArguments != null && typeArguments.Count > 0)
            {
                filter = FilterTypeParameterCount(typeArguments.Count).And(filter);
            }

            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetMethodsImpl(type, typeArguments, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetMethodsImpl(t, typeArguments, filter, options));
            }
        }

        static Predicate<IUnresolvedMethod> FilterTypeParameterCount(int expectedTypeParameterCount)
        {
            return m => m.TypeParameters.Count == expectedTypeParameterCount;
        }

        const GetMemberOptions declaredMembers = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;

        static IEnumerable<IMethod> GetMethodsImpl(IType baseType, IList<IType> methodTypeArguments, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            IEnumerable<IMethod> declaredMethods = baseType.GetMethods(filter, options | declaredMembers);

            ParameterizedType pt = baseType as ParameterizedType;
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == 0
                && (pt != null || (methodTypeArguments != null && methodTypeArguments.Count > 0)))
            {
                TypeParameterSubstitution substitution = null;
                foreach (IMethod m in declaredMethods)
                {
                    if (methodTypeArguments != null && methodTypeArguments.Count > 0)
                    {
                        if (m.TypeParameters.Count != methodTypeArguments.Count)
                            continue;
                    }
                    if (substitution == null)
                    {
                        if (pt != null)
                            substitution = pt.GetSubstitution(methodTypeArguments);
                        else
                            substitution = new TypeParameterSubstitution(null, methodTypeArguments);
                    }
                    yield return new SpecializedMethod(m, substitution);
                }
            }
            else
            {
                foreach (IMethod m in declaredMethods)
                {
                    yield return m;
                }
            }
        }
        #endregion

        #region GetAccessors
        public static IEnumerable<IMethod> GetAccessors(IType type, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetAccessorsImpl(type, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetAccessorsImpl(t, filter, options));
            }
        }

        static IEnumerable<IMethod> GetAccessorsImpl(IType baseType, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            return GetConstructorsOrAccessorsImpl(baseType, baseType.GetAccessors(filter, options | declaredMembers), filter, options);
        }
        #endregion

        #region GetConstructors
        public static IEnumerable<IMethod> GetConstructors(IType type, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetConstructorsImpl(type, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetConstructorsImpl(t, filter, options));
            }
        }

        static IEnumerable<IMethod> GetConstructorsImpl(IType baseType, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            return GetConstructorsOrAccessorsImpl(baseType, baseType.GetConstructors(filter, options | declaredMembers), filter, options);
        }

        static IEnumerable<IMethod> GetConstructorsOrAccessorsImpl(IType baseType, IEnumerable<IMethod> declaredMembers, Predicate<IUnresolvedMethod> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
            {
                return declaredMembers;
            }

            ParameterizedType pt = baseType as ParameterizedType;
            if (pt != null)
            {
                var substitution = pt.GetSubstitution();
                return declaredMembers.Select(m => new SpecializedMethod(m, substitution) { DeclaringType = pt });
            }
            else
            {
                return declaredMembers;
            }
        }
        #endregion

        #region GetProperties
        public static IEnumerable<IProperty> GetProperties(IType type, Predicate<IUnresolvedProperty> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetPropertiesImpl(type, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetPropertiesImpl(t, filter, options));
            }
        }

        static IEnumerable<IProperty> GetPropertiesImpl(IType baseType, Predicate<IUnresolvedProperty> filter, GetMemberOptions options)
        {
            IEnumerable<IProperty> declaredProperties = baseType.GetProperties(filter, options | declaredMembers);
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
            {
                return declaredProperties;
            }

            ParameterizedType pt = baseType as ParameterizedType;
            if (pt != null)
            {
                var substitution = pt.GetSubstitution();
                return declaredProperties.Select(m => new SpecializedProperty(m, substitution) { DeclaringType = pt });
            }
            else
            {
                return declaredProperties;
            }
        }
        #endregion

        #region GetFields
        public static IEnumerable<IField> GetFields(IType type, Predicate<IUnresolvedField> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetFieldsImpl(type, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetFieldsImpl(t, filter, options));
            }
        }

        static IEnumerable<IField> GetFieldsImpl(IType baseType, Predicate<IUnresolvedField> filter, GetMemberOptions options)
        {
            IEnumerable<IField> declaredFields = baseType.GetFields(filter, options | declaredMembers);
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
            {
                return declaredFields;
            }

            ParameterizedType pt = baseType as ParameterizedType;
            if (pt != null)
            {
                var substitution = pt.GetSubstitution();
                return declaredFields.Select(m => new SpecializedField(m, substitution) { DeclaringType = pt });
            }
            else
            {
                return declaredFields;
            }
        }
        #endregion

        #region GetEvents
        public static IEnumerable<IEvent> GetEvents(IType type, Predicate<IUnresolvedEvent> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetEventsImpl(type, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetEventsImpl(t, filter, options));
            }
        }

        static IEnumerable<IEvent> GetEventsImpl(IType baseType, Predicate<IUnresolvedEvent> filter, GetMemberOptions options)
        {
            IEnumerable<IEvent> declaredEvents = baseType.GetEvents(filter, options | declaredMembers);
            if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions)
            {
                return declaredEvents;
            }

            ParameterizedType pt = baseType as ParameterizedType;
            if (pt != null)
            {
                var substitution = pt.GetSubstitution();
                return declaredEvents.Select(m => new SpecializedEvent(m, substitution) { DeclaringType = pt });
            }
            else
            {
                return declaredEvents;
            }
        }
        #endregion

        #region GetMembers
        public static IEnumerable<IMember> GetMembers(IType type, Predicate<IUnresolvedMember> filter, GetMemberOptions options)
        {
            if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers)
            {
                return GetMembersImpl(type, filter, options);
            }
            else
            {
                return type.GetNonInterfaceBaseTypes().SelectMany(t => GetMembersImpl(t, filter, options));
            }
        }

        static IEnumerable<IMember> GetMembersImpl(IType baseType, Predicate<IUnresolvedMember> filter, GetMemberOptions options)
        {
            foreach (var m in GetMethodsImpl(baseType, null, filter, options))
                yield return m;
            foreach (var m in GetPropertiesImpl(baseType, filter, options))
                yield return m;
            foreach (var m in GetFieldsImpl(baseType, filter, options))
                yield return m;
            foreach (var m in GetEventsImpl(baseType, filter, options))
                yield return m;
        }
        #endregion
    }
}

namespace ICSharpCode.NRefactory.Utils
{
    public sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public readonly static ReferenceComparer Instance = new ReferenceComparer();

        public new bool Equals(object x, object y)
        {
            return x == y;
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
