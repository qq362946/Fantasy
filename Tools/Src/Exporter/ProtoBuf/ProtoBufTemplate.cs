namespace Fantasy.Exporter;

public static class ProtoBuffTemplate
{
    public static readonly string Template ="""
                                            #if SERVER
                                            using ProtoBuf;
                                            using System.Collections.Generic;
                                            using Fantasy;
                                            #pragma warning disable CS8618
                                            
                                            namespace Fantasy
                                            {	
                                            #else
                                            using ProtoBuf;
                                            using System.Collections.Generic;
                                            using Fantasy.Core.Network;
                                            #pragma warning disable CS8618
                                            
                                            namespace Fantasy
                                            {
                                            #endif
                                            (Content)}
                                            """;
}