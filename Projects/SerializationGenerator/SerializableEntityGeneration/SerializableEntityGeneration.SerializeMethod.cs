/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.SerializeMethod.cs                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public static partial class SerializableEntityGeneration
    {
        public static void GenerateSerializeMethod(
            this StringBuilder source,
            Compilation compilation,
            bool isOverride,
            ImmutableArray<IFieldSymbol> fields,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var genericWriterInterface = compilation.GetTypeByMetadataName(GENERIC_WRITER_INTERFACE);

            source.GenerateMethodStart(
                "Serialize",
                AccessModifier.Public,
                isOverride,
                "void",
                ImmutableArray.Create<(ITypeSymbol, string)>((genericWriterInterface, "writer"))
            );

            const string indent = "            ";

            source.AppendLine(@$"{indent}if (SavePosition > -1)
{indent}{{
{indent}    writer.Seek(SavePosition, SeekOrigin.Begin);
{indent}    return;
{indent}}}");

            // Version
            source.AppendLine($"{indent}writer.WriteEncodedInt(_version);");

            foreach (var field in fields)
            {
                source.SerializeField($"{indent}    ", field, compilation, serializableTypes);
            }

            source.GenerateMethodEnd();
        }

        public static void SerializeField(
            this StringBuilder source,
            string indent,
            IFieldSymbol field,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var fieldName = field.Name;
            var fieldType = field.Type;

            if (fieldType.IsEnum())
            {
                source.AppendLine($"{indent}writer.WriteEnum({fieldName})");
                return;
            }

            // Uses `writer.Write(obj);`
            var primitiveWrite = fieldType.IsPrimitiveSerialization(compilation, serializableTypes);

            var attributes = field.GetAttributes();

            if (primitiveWrite)
            {
                if (attributes.Any(a => a.IsDeltaDateTime(compilation)))
                {
                    source.AppendLine($"{indent}writer.WriteDeltaTime({fieldName})");
                }
                return;
            }

            throw new Exception($"No serialization Write method for type {field}");
        }

        private static bool IsPrimitiveSerialization(
            this ITypeSymbol symbol,
            Compilation compilation,
            ImmutableArray<INamedTypeSymbol> serializableTypes
        )
        {
            var isSpecialType = symbol.SpecialType switch
            {
                SpecialType.System_Boolean   => true,
                SpecialType.System_SByte     => true,
                SpecialType.System_Int16     => true,
                SpecialType.System_Int32     => true,
                SpecialType.System_Int64     => true,
                SpecialType.System_Byte      => true,
                SpecialType.System_UInt16    => true,
                SpecialType.System_UInt32    => true,
                SpecialType.System_UInt64    => true,
                SpecialType.System_Single    => true,
                SpecialType.System_Double    => true,
                SpecialType.System_String    => true,
                SpecialType.System_Decimal   => true,
                SpecialType.System_DateTime  => true,
                SpecialType.System_ValueType => true,
                _                            => false
            };

            return isSpecialType ||
                   symbol.IsPoint2D(compilation) ||
                   symbol.IsPoint3D(compilation) ||
                   symbol.IsRectangle2D(compilation) ||
                   symbol.IsRectangle3D(compilation) ||
                   symbol.IsIpAddress(compilation) ||
                   symbol.IsRace(compilation) ||
                   symbol.IsMap(compilation) ||
                   // Already inherits `ISerializable` somehow
                   symbol.HasSerializableInterface(compilation) ||
                   // Will be serialized and possibly have `ISerializable` added to it
                   symbol is INamedTypeSymbol namedSymbol &&
                   serializableTypes.Contains(namedSymbol, SymbolEqualityComparer.Default);
        }
    }
}
