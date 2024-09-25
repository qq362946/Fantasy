namespace Fantasy.Exporter;

public static class NetworkProtocolTemplate
{
    public static readonly string Template ="""
                                            #if SERVER
                                            using ProtoBuf;
                                            (UsingNamespace)
                                            using System.Collections.Generic;
                                            using MongoDB.Bson.Serialization.Attributes;
                                            using Fantasy;
                                            using Fantasy.Network.Interface;
                                            using Fantasy.Serialize;
                                            // ReSharper disable InconsistentNaming
                                            // ReSharper disable RedundantUsingDirective
                                            // ReSharper disable RedundantOverriddenMember
                                            // ReSharper disable PartialTypeWithSinglePart
                                            // ReSharper disable UnusedAutoPropertyAccessor.Global
                                            // ReSharper disable MemberCanBePrivate.Global
                                            // ReSharper disable CheckNamespace
                                            #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                                            #pragma warning disable CS8618
                                            
                                            namespace Fantasy
                                            {	
                                            #else
                                            using ProtoBuf;
                                            (UsingNamespace)
                                            using System.Collections.Generic;
                                            using Fantasy;
                                            using Fantasy.Network.Interface;
                                            using Fantasy.Serialize;
                                            #pragma warning disable CS8618
                                            
                                            namespace Fantasy
                                            {
                                            #endif
                                            (Content)}
                                            """;
}