using Detekonai.Core.Common;
using Detekonai.Networking.NetSync.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Detekonai.Networking.NetSync.Injector.Editor
{
    public class NetSyncInjector
    {
        private static readonly OpCode[] argumentLoadMap =
{
            OpCodes.Ldarg_0,
            OpCodes.Ldarg_1,
            OpCodes.Ldarg_2,
            OpCodes.Ldarg_3,
        };
        private static readonly OpCode[] intLoadMap =
        {
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

        private static readonly OpCode[] stlocLoadMap =
        {
            OpCodes.Stloc_0,
            OpCodes.Stloc_1,
            OpCodes.Stloc_2,
            OpCodes.Stloc_3,
        };

        private static readonly OpCode[] ldlocLoadMap =
{
            OpCodes.Ldloc_0,
            OpCodes.Ldloc_1,
            OpCodes.Ldloc_2,
            OpCodes.Ldloc_3,
        };

        private readonly AssemblyDefinition injectorAssembly;
        private readonly MethodDefinition writeInterceptor;
        private readonly MethodDefinition callInterceptor;
        private readonly string thisLocation;
        private readonly string interceptorLocation;
        private readonly ILogConnector logger;
        private readonly TypeReference netSyncAttribute;
        private readonly TypeReference netSyncIgnoreAttribute;
        public NetSyncInjector(ILogConnector logger)
        {
            this.logger = logger;
            thisLocation = Assembly.GetAssembly(typeof(NetSyncInjector)).Location;
            interceptorLocation = Assembly.GetAssembly(typeof(INetworkInterceptor)).Location;
            injectorAssembly = AssemblyDefinition.ReadAssembly(thisLocation);
            AssemblyDefinition interceptorAssembly = AssemblyDefinition.ReadAssembly(interceptorLocation);
            writeInterceptor = interceptorAssembly.MainModule.Types.Single(x => x.Name == "INetworkInterceptor").Methods.Single(y => y.Name == "WriteValue");
            callInterceptor = interceptorAssembly.MainModule.Types.Single(x => x.Name == "INetworkInterceptor").Methods.Single(y => y.Name == "CallFunction");
            netSyncAttribute = interceptorAssembly.MainModule.ImportReference(typeof(NetSyncAttribute));
            netSyncIgnoreAttribute = interceptorAssembly.MainModule.ImportReference(typeof(NetSyncIgnoreAttribute));
        }

        public void Inject(string fileName, string includeDir)
        {
            try
            {
                if (fileName == thisLocation || fileName == interceptorLocation)
                {
                    logger?.Log(this, $"We don't inject to local assemblies!");
                    return;
                }
                logger?.Log(this, $"Local is {thisLocation} {fileName}");
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(includeDir);

                using (FileStream assemblyStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (var module = ModuleDefinition.ReadModule(assemblyStream, new ReaderParameters { ReadWrite = true, AssemblyResolver = resolver }))
                    {
                        logger?.Log(this, $"Local is {injectorAssembly.Name}");

                        //this may be slow?
                        var types = module.Types.Where(x => x.IsClass && x.Properties.Where(y => y.PropertyType.FullName == "Detekonai.Networking.NetSync.Runtime.INetworkInterceptor").Any());
                        types = types.Union(module.Types.SelectMany(x => x.NestedTypes).Where(z => z.IsClass && z.Properties.Where(y => y.PropertyType.FullName == "Detekonai.Networking.NetSync.Runtime.INetworkInterceptor").Any()));

                        logger?.Log(this, $"Inject begin for {module.Name}");
                        if (types.Any())
                        {
                            logger?.Log(this, $"-Found NetSync objects");
                            foreach (var type in types)
                            {
                                string typeName = type.Name;
                                CustomAttribute attrib = type.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == netSyncIgnoreAttribute.FullName);
                                if (attrib == null) //ignore any class with NetSyncIgnore
                                {
                                    attrib = type.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == netSyncAttribute.FullName);
                                    if (attrib != null)
                                    {
                                        var argument = attrib.Properties.FirstOrDefault(x => x.Name == nameof(NetSyncAttribute.Name));
                                        if (!string.IsNullOrEmpty((string)argument.Argument.Value))
                                        {
                                            typeName = (string)argument.Argument.Value;
                                        }
                                    }
                                    var interceptorProperty = type.Properties.Where(x => x.PropertyType.FullName == "Detekonai.Networking.NetSync.Runtime.INetworkInterceptor").Single();
                                    var interceptorGetterRef = type.Module.ImportReference(interceptorProperty.GetMethod);
                                    logger?.Log(this, $"---Injecting to {type.Name}");
                                    InjectWriteInterceptors(typeName, type, interceptorGetterRef, writeInterceptor);
                                    InjectMethodInterceptors(typeName, type, interceptorGetterRef, callInterceptor);
                                }
                            }

                            module.Write(); // Write to the same file that was used to open the file
                        }
                        logger?.Log(this, $"Inject end for {module.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                var st = new StackTrace(ex, true);
                var line = 0;
                var fn = "";
                for (int i = 0; i < st.FrameCount; i++)
                {
                    var frame = st.GetFrame(i);
                    if (frame.GetMethod().Module.Assembly == Assembly.GetAssembly(typeof(NetSyncInjector)))
                    {
                        line = frame.GetFileLineNumber();
                        fn = frame.GetFileName();
                        break;
                    }
                }
                if (fn == "")
                {
                    var frame = st.GetFrame(0);
                    line = frame.GetFileLineNumber();
                    fn = frame.GetFileName();
                }
                Console.WriteLine($"{fn}({line}) : error NETSYNCEX : {ex.Message}");
                throw;
            }
        }

        private static void InjectInterceptor(ILProcessor processor, Instruction last, Instruction mainStart, MethodReference interceptorGetter)
        {
            var newInstruction = processor.Create(OpCodes.Ldarg_0); //load this
            processor.InsertBefore(last, newInstruction);
            newInstruction = processor.Create(OpCodes.Call, interceptorGetter); //call getter on this
            processor.InsertBefore(last, newInstruction);
            newInstruction = processor.Create(OpCodes.Dup); //dup the interceptor so we can do null check
            processor.InsertBefore(last, newInstruction);
            newInstruction = processor.Create(OpCodes.Brtrue_S, mainStart); //jump to start if the interceptor is not null
            processor.InsertBefore(last, newInstruction);
            newInstruction = processor.Create(OpCodes.Pop); // pop the interceptor from the stack
            processor.InsertBefore(last, newInstruction);
            newInstruction = processor.Create(OpCodes.Br_S, last); // the interceptor is null jump to end
            processor.InsertBefore(last, newInstruction);
        }

        private void InjectWriteInterceptors(string obName, TypeDefinition type, MethodReference interceptorGetter, MethodDefinition wInterceptor)
        {
            foreach (var prop in type.Properties)
            {
                CustomAttribute attrib = prop.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == netSyncIgnoreAttribute.FullName);
                if (attrib == null) //ignore properties with NetSyncIgnore
                {
                    if (prop.GetMethod == interceptorGetter)
                    {
                        continue;
                    }
                    if (prop.SetMethod == null)
                    {
                        continue;
                    }
                    logger?.Log(this, $"------Intercepting {prop.Name} {prop.PropertyType}");

                    var writeMethod = prop.SetMethod.DeclaringType.Module.ImportReference(wInterceptor);

                    var processor = prop.SetMethod.Body.GetILProcessor();
                    var first = processor.Body.Instructions.First();//get the last instruction which shoud be a return
                    var startInterceptOp = processor.Create(OpCodes.Ldstr, obName);//we will start the interceptor with this, loading the object name to the stack, note we didn't inserted it yet!
                    InjectInterceptor(processor, first, startInterceptOp, interceptorGetter);//inject interceptor check if it's not null
                                                                                            //intercept
                    processor.InsertBefore(first, startInterceptOp);//insert the start instruction we created before
                    var newInstruction = processor.Create(OpCodes.Ldstr, prop.Name);//load the property name
                    processor.InsertBefore(first, newInstruction);
                    newInstruction = processor.Create(OpCodes.Ldarg_1);//load the value
                    processor.InsertBefore(first, newInstruction);
                    newInstruction = processor.Create(OpCodes.Box, prop.PropertyType);//boxing
                    processor.InsertBefore(first, newInstruction);
                    newInstruction = processor.Create(OpCodes.Callvirt, writeMethod);//call the interceptor
                    processor.InsertBefore(first, newInstruction);
                }
            }
        }

        private void InjectMethodInterceptors(string obName, TypeDefinition type, MethodReference interceptorGetter, MethodDefinition callInterceptor)
        {
            foreach (var method in type.Methods)
            {
                var attrib = method.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == netSyncAttribute.FullName);
                if (attrib != null)
                {
                    var argument = attrib.Properties.FirstOrDefault(x => x.Name == nameof(NetSyncAttribute.Name));
                    string methodAlias = method.Name;
                    if (!string.IsNullOrEmpty((string)argument.Argument.Value))
                    {
                        methodAlias = (string)argument.Argument.Value;
                    }
                    logger?.Log(this, $"------Initializing {method.Name} as {methodAlias}");
                    if (method.Parameters.Count > 255)
                    {
                        throw new InvalidProgramException($"Method {method}({methodAlias}) in object {obName} marked as NetSync but has too many parameters, remove NetSync marker or use less than 256 parameters!");
                    }
                    var writeMethod = method.DeclaringType.Module.ImportReference(callInterceptor);
                    var objectType = method.DeclaringType.Module.ImportReference(typeof(object));
                    var processor = method.Body.GetILProcessor();

                    //setup array local variable
                    method.Body.InitLocals = true;
                    var arrVar = new VariableDefinition(objectType);
                    method.Body.Variables.Add(arrVar);

                    var first = processor.Body.Instructions.First();//get the last instruction which shoud be a return
                    var startInterceptOp = LoadNumber(processor, method.Parameters.Count);//we will start the interceptor with this, loading the parameter count to the stack, note we didn't inserted it yet!
                    InjectInterceptor(processor, first, startInterceptOp, interceptorGetter);//inject interceptor check if it's not null

                    processor.InsertBefore(first, startInterceptOp);//insert the start instruction we created before 
                    var newInstruction = processor.Create(OpCodes.Newarr, objectType);//create the parameter array
                    processor.InsertBefore(first, newInstruction);
                    newInstruction = StoreVariable(processor, arrVar.Index);//put the array to it's variable
                    processor.InsertBefore(first, newInstruction);
                    for (int i = 0; i < method.Parameters.Count; i++)
                    {
                        newInstruction = LoadVariable(processor, arrVar.Index);//put the array from local variable to stack
                        processor.InsertBefore(first, newInstruction);
                        newInstruction = LoadNumber(processor, i);//put the current index to the stack
                        processor.InsertBefore(first, newInstruction);
                        newInstruction = LoadArgument(processor, (byte)(i + 1));//load the next value
                        processor.InsertBefore(first, newInstruction);
                        newInstruction = processor.Create(OpCodes.Box, method.Parameters[i].ParameterType);//boxing
                        processor.InsertBefore(first, newInstruction);

                        newInstruction = processor.Create(OpCodes.Stelem_Ref);//put the value to the array
                        processor.InsertBefore(first, newInstruction);
                    }
                    newInstruction = processor.Create(OpCodes.Ldstr, obName);//load the type name
                    processor.InsertBefore(first, newInstruction);
                    newInstruction = processor.Create(OpCodes.Ldstr, methodAlias);//load the method name
                    processor.InsertBefore(first, newInstruction);
                    newInstruction = LoadVariable(processor, arrVar.Index);//put the array from local variable to stack
                    processor.InsertBefore(first, newInstruction);

                    newInstruction = processor.Create(OpCodes.Callvirt, writeMethod);//call the interceptor
                    processor.InsertBefore(first, newInstruction);
                }
            }
        }

        private static Instruction StoreVariable(ILProcessor processor, int argumentIdx)
        {
            if (argumentIdx < stlocLoadMap.Length)
            {
                return processor.Create(stlocLoadMap[argumentIdx]);
            }
            else
            {
                return processor.Create(OpCodes.Stloc, argumentIdx);
            }
        }

        private static Instruction LoadVariable(ILProcessor processor, int argumentIdx)
        {
            if (argumentIdx < ldlocLoadMap.Length)
            {
                return processor.Create(ldlocLoadMap[argumentIdx]);
            }
            else
            {
                return processor.Create(OpCodes.Ldloc, argumentIdx);
            }
        }

        private static Instruction LoadArgument(ILProcessor processor, byte argumentIdx)
        {
            if (argumentIdx < argumentLoadMap.Length)
            {
                return processor.Create(argumentLoadMap[argumentIdx]);
            }
            else
            {
                return processor.Create(OpCodes.Ldarg_S, argumentIdx);
            }
        }

        private static Instruction LoadNumber(ILProcessor processor, int number)
        {
            if (number < intLoadMap.Length)
            {
                return processor.Create(intLoadMap[number]);
            }
            else
            {
                return processor.Create(OpCodes.Ldc_I4, number);
            }
        }
    }


}
