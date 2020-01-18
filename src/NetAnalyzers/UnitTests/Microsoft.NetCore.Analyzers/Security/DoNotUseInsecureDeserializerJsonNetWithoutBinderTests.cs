﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Security.DoNotUseInsecureDeserializerJsonNetWithoutBinder,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Security.DoNotUseInsecureDeserializerJsonNetWithoutBinder,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.NetCore.Analyzers.Security.UnitTests
{
    [Trait(Traits.DataflowAnalysis, Traits.Dataflow.PropertySetAnalysis)]
    public class DoNotUseInsecureDeserializerJsonNetWithoutBinderTests
    {
        private static readonly DiagnosticDescriptor DefinitelyRule =
            DoNotUseInsecureDeserializerJsonNetWithoutBinder.DefinitelyInsecureSerializer;
        private static readonly DiagnosticDescriptor MaybeRule =
            DoNotUseInsecureDeserializerJsonNetWithoutBinder.MaybeInsecureSerializer;

        [Fact]
        public async Task DocSample1_CSharp_Violation()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using Newtonsoft.Json;

public class BookRecord
{
    public string Title { get; set; }
    public object Location { get; set; }
}

public abstract class Location
{
    public string StoreId { get; set; }
}

public class AisleLocation : Location
{
    public char Aisle { get; set; }
    public byte Shelf { get; set; }
}

public class WarehouseLocation : Location
{
    public string Bay { get; set; }
    public byte Shelf { get; set; }
}

public class ExampleClass
{
    public BookRecord DeserializeBookRecord(JsonReader reader)
    {
        JsonSerializer jsonSerializer = new JsonSerializer();
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto;
        return jsonSerializer.Deserialize<BookRecord>(reader);    // CA2329 violation
    }
}
",
            GetCSharpResultAt(33, 16, DefinitelyRule));
        }

        [Fact]
        public async Task DocSample1_VB_Violation()
        {
            await VerifyBasicWithJsonNetAsync(@"
Imports Newtonsoft.Json

Public Class BookRecord
    Public Property Title As String
    Public Property Location As Location
End Class

Public MustInherit Class Location
    Public Property StoreId As String
End Class

Public Class AisleLocation
    Inherits Location

    Public Property Aisle As Char
    Public Property Shelf As Byte
End Class

Public Class WarehouseLocation
    Inherits Location

    Public Property Bay As String
    Public Property Shelf As Byte
End Class

Public Class ExampleClass
    Public Function DeserializeBookRecord(reader As JsonReader) As BookRecord
        Dim jsonSerializer As JsonSerializer = New JsonSerializer()
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto
        Return JsonSerializer.Deserialize(Of BookRecord)(reader)    ' CA2329 violation
    End Function
End Class
",
                GetBasicResultAt(31, 16, DefinitelyRule));
        }

        [Fact]
        public async Task DocSample1_CSharp_Solution()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class BookRecordSerializationBinder : ISerializationBinder
{
    // To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    private static readonly DefaultSerializationBinder Binder = new DefaultSerializationBinder();

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        Binder.BindToName(serializedType, out assemblyName, out typeName);
    }

    public Type BindToType(string assemblyName, string typeName)
    {
        // If the type isn't expected, then stop deserialization.
        if (typeName != ""BookRecord"" && typeName != ""AisleLocation"" && typeName != ""WarehouseLocation"")
        {
            return null;
        }

        return Binder.BindToType(assemblyName, typeName);
    }
}

public class BookRecord
{
    public string Title { get; set; }
    public object Location { get; set; }
}

public abstract class Location
{
    public string StoreId { get; set; }
}

public class AisleLocation : Location
{
    public char Aisle { get; set; }
    public byte Shelf { get; set; }
}

public class WarehouseLocation : Location
{
    public string Bay { get; set; }
    public byte Shelf { get; set; }
}

public class ExampleClass
{
    public BookRecord DeserializeBookRecord(JsonReader reader)
    {
        JsonSerializer jsonSerializer = new JsonSerializer();
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto;
        jsonSerializer.SerializationBinder = new BookRecordSerializationBinder();
        return jsonSerializer.Deserialize<BookRecord>(reader);
    }
}
");
        }

        [Fact]
        public async Task DocSample1_VB_Solution()
        {
            await VerifyBasicWithJsonNetAsync(@"
Imports System
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Serialization

Public Class BookRecordSerializationBinder
    Implements ISerializationBinder

    ' To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    Private Shared ReadOnly Property Binder As New DefaultSerializationBinder()

    Public Sub BindToName(serializedType As Type, ByRef assemblyName As String, ByRef typeName As String) Implements ISerializationBinder.BindToName
        Binder.BindToName(serializedType, assemblyName, typeName)
    End Sub

    Public Function BindToType(assemblyName As String, typeName As String) As Type Implements ISerializationBinder.BindToType
        ' If the type isn't expected, then stop deserialization.
        If typeName <> ""BookRecord"" AndAlso typeName <> ""AisleLocation"" AndAlso typeName <> ""WarehouseLocation"" Then
            Return Nothing
        End If

        Return Binder.BindToType(assemblyName, typeName)
    End Function
End Class

Public Class BookRecord
    Public Property Title As String
    Public Property Location As Location
End Class

Public MustInherit Class Location
    Public Property StoreId As String
End Class

Public Class AisleLocation
    Inherits Location

    Public Property Aisle As Char
    Public Property Shelf As Byte
End Class

Public Class WarehouseLocation
    Inherits Location

    Public Property Bay As String
    Public Property Shelf As Byte
End Class

Public Class ExampleClass
    Public Function DeserializeBookRecord(reader As JsonReader) As BookRecord
        Dim jsonSerializer As JsonSerializer = New JsonSerializer()
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto
        jsonSerializer.SerializationBinder = New BookRecordSerializationBinder()
        Return jsonSerializer.Deserialize(Of BookRecord)(reader)
    End Function
End Class
");
        }

        [Fact]
        public async Task DocSample2_CSharp_Violation()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class BookRecordSerializationBinder : ISerializationBinder
{
    // To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    private static readonly DefaultSerializationBinder Binder = new DefaultSerializationBinder();

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        Binder.BindToName(serializedType, out assemblyName, out typeName);
    }

    public Type BindToType(string assemblyName, string typeName)
    {
        // If the type isn't expected, then stop deserialization.
        if (typeName != ""BookRecord"" && typeName != ""AisleLocation"" && typeName != ""WarehouseLocation"")
        {
            return null;
        }

        return Binder.BindToType(assemblyName, typeName);
    }
}

public class BookRecord
{
    public string Title { get; set; }
    public object Location { get; set; }
}

public abstract class Location
{
    public string StoreId { get; set; }
}

public class AisleLocation : Location
{
    public char Aisle { get; set; }
    public byte Shelf { get; set; }
}

public class WarehouseLocation : Location
{
    public string Bay { get; set; }
    public byte Shelf { get; set; }
}

public static class Binders
{
    public static ISerializationBinder BookRecord = new BookRecordSerializationBinder();
}

public class ExampleClass
{
    public BookRecord DeserializeBookRecord(JsonReader reader)
    {
        JsonSerializer jsonSerializer = new JsonSerializer();
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto;
        jsonSerializer.SerializationBinder = Binders.BookRecord;
        return jsonSerializer.Deserialize<BookRecord>(reader);    // CA2330 -- SerializationBinder might be null
    }
}
",
                GetCSharpResultAt(63, 16, MaybeRule));
        }

        [Fact]
        public async Task DocSample2_VB_Violation()
        {
            await VerifyBasicWithJsonNetAsync(@"
Imports System
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Serialization

Public Class BookRecordSerializationBinder
    Implements ISerializationBinder

    ' To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    Private Shared ReadOnly Property Binder As New DefaultSerializationBinder()

    Public Sub BindToName(serializedType As Type, ByRef assemblyName As String, ByRef typeName As String) Implements ISerializationBinder.BindToName
        Binder.BindToName(serializedType, assemblyName, typeName)
    End Sub

    Public Function BindToType(assemblyName As String, typeName As String) As Type Implements ISerializationBinder.BindToType
        ' If the type isn't expected, then stop deserialization.
        If typeName <> ""BookRecord"" AndAlso typeName <> ""AisleLocation"" AndAlso typeName <> ""WarehouseLocation"" Then
            Return Nothing
        End If

        Return Binder.BindToType(assemblyName, typeName)
    End Function
End Class

Public Class BookRecord
    Public Property Title As String
    Public Property Location As Location
End Class

Public MustInherit Class Location
    Public Property StoreId As String
End Class

Public Class AisleLocation
    Inherits Location

    Public Property Aisle As Char
    Public Property Shelf As Byte
End Class

Public Class WarehouseLocation
    Inherits Location

    Public Property Bay As String
    Public Property Shelf As Byte
End Class

Public Class Binders
    Public Shared Property BookRecord As ISerializationBinder = New BookRecordSerializationBinder()
End Class

Public Class ExampleClass
    Public Function DeserializeBookRecord(reader As JsonReader) As BookRecord
        Dim jsonSerializer As JsonSerializer = New JsonSerializer()
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto
        jsonSerializer.SerializationBinder = Binders.BookRecord
        Return jsonSerializer.Deserialize(Of BookRecord)(reader)    ' CA2330 -- SerializationBinder might be null
    End Function
End Class
",
                GetBasicResultAt(58, 16, MaybeRule));
        }

        [Fact]
        public async Task DocSample2_CSharp_Solution()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class BookRecordSerializationBinder : ISerializationBinder
{
    // To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    private static readonly DefaultSerializationBinder Binder = new DefaultSerializationBinder();

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        Binder.BindToName(serializedType, out assemblyName, out typeName);
    }

    public Type BindToType(string assemblyName, string typeName)
    {
        // If the type isn't expected, then stop deserialization.
        if (typeName != ""BookRecord"" && typeName != ""AisleLocation"" && typeName != ""WarehouseLocation"")
        {
            return null;
        }

        return Binder.BindToType(assemblyName, typeName);
    }
}

public class BookRecord
{
    public string Title { get; set; }
    public object Location { get; set; }
}

public abstract class Location
{
    public string StoreId { get; set; }
}

public class AisleLocation : Location
{
    public char Aisle { get; set; }
    public byte Shelf { get; set; }
}

public class WarehouseLocation : Location
{
    public string Bay { get; set; }
    public byte Shelf { get; set; }
}

public static class Binders
{
    public static ISerializationBinder BookRecord = new BookRecordSerializationBinder();
}

public class ExampleClass
{
    public BookRecord DeserializeBookRecord(JsonReader reader)
    {
        JsonSerializer jsonSerializer = new JsonSerializer();
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto;

        // Ensure that SerializationBinder is assigned non-null before deserializing
        jsonSerializer.SerializationBinder = Binders.BookRecord ?? throw new Exception(""Expected non-null"");

        return jsonSerializer.Deserialize<BookRecord>(reader);
    }
}
");
        }

        [Fact]
        public async Task DocSample2_VB_Solution()
        {
            await VerifyBasicWithJsonNetAsync(@"
Imports System
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Serialization

Public Class BookRecordSerializationBinder
    Implements ISerializationBinder

    ' To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    Private Shared ReadOnly Property Binder As New DefaultSerializationBinder()

    Public Sub BindToName(serializedType As Type, ByRef assemblyName As String, ByRef typeName As String) Implements ISerializationBinder.BindToName
        Binder.BindToName(serializedType, assemblyName, typeName)
    End Sub

    Public Function BindToType(assemblyName As String, typeName As String) As Type Implements ISerializationBinder.BindToType
        ' If the type isn't expected, then stop deserialization.
        If typeName <> ""BookRecord"" AndAlso typeName <> ""AisleLocation"" AndAlso typeName <> ""WarehouseLocation"" Then
            Return Nothing
        End If

        Return Binder.BindToType(assemblyName, typeName)
    End Function
End Class

Public Class BookRecord
    Public Property Title As String
    Public Property Location As Location
End Class

Public MustInherit Class Location
    Public Property StoreId As String
End Class

Public Class AisleLocation
    Inherits Location

    Public Property Aisle As Char
    Public Property Shelf As Byte
End Class

Public Class WarehouseLocation
    Inherits Location

    Public Property Bay As String
    Public Property Shelf As Byte
End Class

Public Class Binders
    Public Shared Property BookRecord As ISerializationBinder = New BookRecordSerializationBinder()
End Class

Public Class ExampleClass
    Public Function DeserializeBookRecord(reader As JsonReader) As BookRecord
        Dim jsonSerializer As JsonSerializer = New JsonSerializer()
        jsonSerializer.TypeNameHandling = TypeNameHandling.Auto

        ' Ensure SerializationBinder is non-null before deserializing
        jsonSerializer.SerializationBinder = If(Binders.BookRecord, New Exception(""Expected non-null""))

        Return jsonSerializer.Deserialize(Of BookRecord)(reader)
    End Function
End Class
");
        }

        [Fact]
        public async Task Insecure_JsonSerializer_Deserialize_DefinitelyDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using Newtonsoft.Json;

class Blah
{
    object Method(JsonReader jr)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.TypeNameHandling = TypeNameHandling.All;
        return serializer.Deserialize(jr);
    }
}",
                GetCSharpResultAt(10, 16, DefinitelyRule));
        }

        [Fact]
        public async Task ExplicitlyNone_JsonSerializer_Deserialize_NoDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using Newtonsoft.Json;

class Blah
{
    object Method(JsonReader jr)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.TypeNameHandling = TypeNameHandling.None;
        return serializer.Deserialize(jr);
    }
}");
        }

        [Fact]
        public async Task AllAndBinder_JsonSerializer_Deserialize_NoDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

class Blah
{
    private Func<ISerializationBinder> SbGetter;

    object Method(JsonReader jr)
    {
        ISerializationBinder sb = SbGetter();
        if (sb != null)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.All;
            serializer.SerializationBinder = sb;
            return serializer.Deserialize(jr);
        }
        else
        {
            return null;
        }
    }
}");
        }

        [Fact]
        public async Task InitializeField_JsonSerializer_Diagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using Newtonsoft.Json;

class Blah
{
    JsonSerializer MyJsonSerializer;

    void Init()
    {
        this.MyJsonSerializer = new JsonSerializer();
        this.MyJsonSerializer.TypeNameHandling = TypeNameHandling.All;
    }
}",
                GetCSharpResultAt(10, 9, DefinitelyRule));
        }

        [Fact]
        public async Task Insecure_JsonSerializer_Populate_MaybeDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

class Blah
{
    private Func<ISerializationBinder> SbGetter;

    object Method(JsonReader jr)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.TypeNameHandling = TypeNameHandling.All;
        serializer.SerializationBinder = SbGetter();
        object o = new object();
        serializer.Populate(jr, o);
        return o;
    }
}",
                GetCSharpResultAt(16, 9, MaybeRule));
        }

        [Fact]
        public async Task Insecure_JsonSerializer_DeserializeGeneric_MaybeDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

class Blah
{
    private Func<ISerializationBinder> SbGetter;

    T Method<T>(JsonReader jr)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.TypeNameHandling = TypeNameHandling.All;
        serializer.SerializationBinder = SbGetter();
        return serializer.Deserialize<T>(jr);
    }
}",
                GetCSharpResultAt(15, 16, MaybeRule));
        }

        // Ideally, we'd transfer the JsonSerializerSettings' TypeNameHandling's state to the JsonSerializer's TypeNameHandling's state.
        [Fact]
        public async Task Insecure_JsonSerializer_FromInsecureSettings_DeserializeGeneric_NoDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

class Blah
{
    private Func<ISerializationBinder> SbGetter;

    T Method<T>(JsonReader jr)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Arrays,
        };
        JsonSerializer serializer = JsonSerializer.Create(settings);
        return serializer.Deserialize<T>(jr);
    }
}");
        }

        [Fact]
        public async Task TypeNameHandlingNoneBinderNonNull_JsonSerializer_Populate_NoDiagnostic()
        {
            await VerifyCSharpWithJsonNetAsync(@"
using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

class Blah
{
    private Func<SerializationBinder> SbGetter;

    object Method(JsonReader jr)
    {
        SerializationBinder sb = SbGetter();
        if (sb != null)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Binder = sb;
            object o = new object();
            serializer.Populate(jr, o);
            return o;
        }
        else
        {
            return null;
        }
    }
}");
        }

        [Theory]
        [InlineData("")]
        [InlineData("dotnet_code_quality.excluded_symbol_names = Method")]
        [InlineData(@"dotnet_code_quality.CA2329.excluded_symbol_names = Method
                      dotnet_code_quality.CA2330.excluded_symbol_names = Method")]
        [InlineData("dotnet_code_quality.dataflow.excluded_symbol_names = Method")]
        public async Task EditorConfigConfiguration_ExcludedSymbolNamesOption(string editorConfigText)
        {
            var csharpTest = new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithNewtonsoftJson,
                TestState =
                {
                    Sources =
                    {
                        @"
using Newtonsoft.Json;

class Blah
{
    object Method(JsonReader jr)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.TypeNameHandling = TypeNameHandling.All;
        return serializer.Deserialize(jr);
    }
}"
                    },
                    AdditionalFiles = { (".editorconfig", editorConfigText) }
                },
            };

            if (editorConfigText.Length == 0)
            {
                csharpTest.ExpectedDiagnostics.Add(
                    GetCSharpResultAt(10, 16, DefinitelyRule)
                );
            }

            await csharpTest.RunAsync();
        }

        private async Task VerifyCSharpWithJsonNetAsync(string source, params DiagnosticResult[] expected)
        {
            var csharpTest = new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithNewtonsoftJson,
                TestState =
                {
                    Sources = { source },
                },
            };

            csharpTest.ExpectedDiagnostics.AddRange(expected);

            await csharpTest.RunAsync();
        }

        private async Task VerifyBasicWithJsonNetAsync(string source, params DiagnosticResult[] expected)
        {
            var vbTest = new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithNewtonsoftJson,
                TestState =
                {
                    Sources = { source },
                },
            };

            vbTest.ExpectedDiagnostics.AddRange(expected);

            await vbTest.RunAsync();
        }

        private static DiagnosticResult GetCSharpResultAt(int line, int column, DiagnosticDescriptor rule)
           => VerifyCS.Diagnostic(rule)
               .WithLocation(line, column);

        private static DiagnosticResult GetBasicResultAt(int line, int column, DiagnosticDescriptor rule)
           => VerifyVB.Diagnostic(rule)
               .WithLocation(line, column);
    }
}