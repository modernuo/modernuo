/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: EntityJsonGenerator.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SerializableMigration;

namespace SerializationGenerator
{
    [Generator]
    public class EntitySerializationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SerializerSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SerializerSyntaxReceiver receiver)
            {
                return;
            }

            var compilation = context.Compilation;

            var migrationPath = SerializableMigrationSchema.GetMigrationPath(context);
            var jsonOptions = SerializableMigrationSchema.GetJsonSerializerOptions(compilation);
            // List of types that _will_ become ISerializable
            var serializableList = receiver.SerializableList;

            foreach (var (classSymbol, (serializableAttr, fieldsList)) in receiver.ClassAndFields)
            {
                if (serializableAttr == null)
                {
                    continue;
                }

                string classSource = SerializableEntityGeneration.GenerateSerializationPartialClass(
                    classSymbol,
                    serializableAttr,
                    fieldsList.ToImmutableArray(),
                    compilation,
                    migrationPath,
                    jsonOptions,
                    serializableList
                );

                if (classSource != null)
                {
                    context.AddSource($"{classSymbol.ToDisplayString()}.Serialization.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }
    }
}
