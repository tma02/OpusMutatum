using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpusMutatum {

	public class OpusMutatum {

		// For intermediary or devExe
		static string PathToLightning = "./Last Call BBS.exe";
		static string PathToModdedLightning = "./Last Call BBS.modded.exe";

		// for merge
		static string PathToMonoMod = "./MonoMod.exe";

		// for strings
		static string MainMethodName = "";
		static string StringDeobfName = "#=qAvgTBxwQSbU8nMKfae8ZTNooHMt6RzxpjnyaOJttHWo=.#=qI5wF45wg$Wevb4QR$HM5tA==";
		static string StringDeobfIntermediaryName = "method_517";

		static List<string> MappingPaths = new List<string>();
		static List<string> IntermediaryPaths = new List<string>();
		static List<string> StringsPaths = new List<string>();

		static AssemblyDefinition LightningAssembly, ModdedLightningAssembly;

		static Dictionary<string, string> Intermediary = new Dictionary<string, string>();
		static Dictionary<string, string> Mappings = new Dictionary<string, string>();
		static Dictionary<int, string> Strings = new Dictionary<int, string>();

		// OS enum, since Linux and Mac are different
		public enum OS {
			Windows,
			Linux,
			Mac
		};
		
		public static OS OpSystem = OS.Windows;

		static void Main(string[] args) {
			ArgumentParsingMode current = ArgumentParsingMode.Argument;
			RunAction action = RunAction.Setup;

			switch(Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					OpSystem = OS.Windows;
					break;
				case PlatformID.MacOSX:
					OpSystem = OS.Mac;
					break;
				default:
					OpSystem = OS.Linux;
					break;
			};

			foreach(var arg in args) {
				switch(current) {
					case ArgumentParsingMode.Argument:
						// check if its "run", "strings", "intermediary", merge", "setup", "devExe"
						// or "--mappings", "--intermediary", "--strings", "--lightning", "--monomod", "--intermediaryPath", "--linux", "--mac", --"win"
						if(arg.Equals("run"))
							action = RunAction.Run;
						else if(arg.Equals("strings"))
							action = RunAction.Strings;
						else if(arg.Equals("intermediary"))
							action = RunAction.Intermediary;
						else if(arg.Equals("merge"))
							action = RunAction.Merge;
						else if(arg.Equals("setup"))
							action = RunAction.Setup;
						else if(arg.Equals("devExe"))
							action = RunAction.DevExe;
						else if(arg.Equals("--mappings"))
							current = ArgumentParsingMode.MappingPath;
						else if(arg.Equals("--intermediary"))
							current = ArgumentParsingMode.IntermediaryPath;
						else if(arg.Equals("--strings"))
							current = ArgumentParsingMode.StringsPath;
						else if(arg.Equals("--lightning"))
							current = ArgumentParsingMode.LightningPath;
						else if(arg.Equals("--monomod"))
							current = ArgumentParsingMode.MonoModPath;
						else if(arg.Equals("--mainmethodname"))
							current = ArgumentParsingMode.MainMethodName;
						else if(arg.Equals("--stringdeobfname"))
							current = ArgumentParsingMode.StringDeobfName;
						else if(arg.Equals("--stringdeobfintname"))
							current = ArgumentParsingMode.StringDeobfIntermediaryName;
						else if(arg.Equals("--linux"))
							OpSystem = OS.Linux;
						else if(arg.Equals("--mac"))
							OpSystem = OS.Mac;
						else if(arg.Equals("--win"))
							OpSystem = OS.Windows;
						break;
					case ArgumentParsingMode.MappingPath:
						MappingPaths.Add(arg);
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.IntermediaryPath:
						IntermediaryPaths.Add(arg);
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.StringsPath:
						StringsPaths.Add(arg);
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.LightningPath:
						PathToLightning = arg;
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.MonoModPath:
						PathToMonoMod = arg;
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.MainMethodName:
						MainMethodName = arg;
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.StringDeobfName:
						StringDeobfName = arg;
						current = ArgumentParsingMode.Argument;
						break;
					case ArgumentParsingMode.StringDeobfIntermediaryName:
						StringDeobfIntermediaryName = arg;
						current = ArgumentParsingMode.Argument;
						break;
					default:
						Console.WriteLine("Invalid argument \"" + arg + "\"!");
						break;
				}
			}

			if(StringsPaths.Count == 0) {
				StringsPaths.Add("./StringDumping/out.csv");
				StringsPaths.Add("./out.csv");
			}

			try {
				switch(action) {
					case RunAction.Strings:
						HandleStrings();
						break;
					case RunAction.Intermediary:
						HandleIntermediary();
						break;
					case RunAction.Merge:
						HandleMerge();
						break;
					case RunAction.Setup:
						HandleStrings();
						HandleIntermediary();
						HandleMerge();
						break;
					case RunAction.DevExe:
						HandleDevExe();
						break;
					case RunAction.Run:
					default:
						HandleRun();
						break;
				}
			} catch(Exception e) {
				Console.WriteLine("Error executing task:");
				Console.WriteLine(e.ToString());
			}
			Console.WriteLine("Done.");
			// keep command line open
			Console.ReadKey();
		}

		static void HandleRun() {
			// just run MONOMODDED_IntermediaryLightning.exe
		}

		static void HandleStrings() {
			// TODO: some config file for main method & parse method?
			
			Console.WriteLine("Dumping strings...");
			LoadLightning();
			var module = LightningAssembly.MainModule;
			var ssplit = StringDeobfName.Split('.');
			var parse = module.FindMethod(ssplit[0], ssplit[1]);

			Console.WriteLine("Finding keys...");
			// get all the keys this way
			var refs = new List<Instruction>();
			foreach(var type in CollectNestedTypes(LightningAssembly.MainModule.Types))
				if(type != null)
				foreach(var method in type.Methods)
					if(method != null)
					if(method.HasBody && method.Body != null && method.Body.Instructions != null)
						foreach(var instr in method.Body.Instructions)
							if(instr != null && instr.OpCode != null)
							if(instr.OpCode.Code == Code.Call && instr.Operand is MethodReference operand && operand.Resolve() != null && operand.Resolve().Equals(parse))
								refs.Add(instr);

			var keys = new HashSet<int>();

			foreach(var instr in refs)
				keys.Add((int)instr.Previous.Operand);

			Console.WriteLine($"Found {keys.Count()} string keys");

			var msplit = MainMethodName.Split('.');
			var mainMethod = msplit.Length == 2 ? module.FindMethod(msplit[0], msplit[1]) : module.Assembly.EntryPoint;
			var proc = mainMethod.Body.GetILProcessor();
			var first = proc.Body.Instructions.First();

			var stringt = module.TypeSystem.String;

			// we want String.Concat(String?, String?, String?)
			Console.WriteLine("Resolving Concat method...");
			var concat = module.ImportReference(stringt.Resolve().Methods.First(f => f.Parameters.Count == 3 && f.Parameters.All(p => p.ParameterType.FullName.Equals(stringt.FullName))));
			Console.WriteLine("Getting StreamWriter class...");
			var streamWriter = module.ImportReference(typeof(StreamWriter)).Resolve();
			Console.WriteLine("Getting StreamWriter constructor...");
			var streamWriterConstructor = module.ImportReference(streamWriter.Methods.First(m => m.Name.Equals(".ctor") && m.Parameters.Count() == 1 && m.Parameters.All(param => param.ParameterType.FullName.Equals(stringt.FullName))));
			Console.WriteLine("Getting WriteLine method...");
			var writeLine = module.ImportReference(streamWriter.BaseType.Resolve().Methods.First(m => m.Name.Equals("WriteLine") && m.Parameters.Count == 1 && m.Parameters.All(p => p.ParameterType.FullName.Equals(stringt.FullName))));
			Console.WriteLine("Getting Dispose method...");
			var dispose = module.ImportReference(streamWriter.BaseType.Resolve().FindMethod("Close"));

			Console.WriteLine("Creating string dumper...");
			proc.InsertBefore(first, proc.Create(OpCodes.Ldstr, "./out.csv"));
			proc.InsertBefore(first, proc.Create(OpCodes.Newobj, streamWriterConstructor));
			foreach(var key in keys) {
				proc.InsertBefore(first, proc.Create(OpCodes.Dup));
				proc.InsertBefore(first, proc.Create(OpCodes.Ldstr, key.ToString()));
				proc.InsertBefore(first, proc.Create(OpCodes.Ldstr, "~,~"));
				proc.InsertBefore(first, proc.Create(OpCodes.Ldc_I4, key));
				proc.InsertBefore(first, proc.Create(OpCodes.Call, parse));
				proc.InsertBefore(first, proc.Create(OpCodes.Call, concat));
				proc.InsertBefore(first, proc.Create(OpCodes.Callvirt, writeLine));
			}
			//proc.InsertBefore(first, proc.Create(OpCodes.Ldc_I4_1));
			proc.InsertBefore(first, proc.Create(OpCodes.Callvirt, dispose));
			proc.InsertBefore(first, proc.Create(OpCodes.Ret));

			Directory.CreateDirectory("./StringDumping");
			module.Write("./StringDumping/Last Call BBS.exe");

			// Yells at you if System and Steamworks aren't in the StringDumping directory
			if(OpSystem != OS.Windows && !File.Exists("./StringDumping/System.dll") && !File.Exists("./StringDumping/Steamworks.NET.dll")) {
				File.Copy("./System.dll", "./StringDumping/System.dll");
				File.Copy("./Steamworks.NET.dll", "./StringDumping/Steamworks.NET.dll");
			}
			Console.WriteLine("Running string dumper...");
			// run the string dumper automatically
			RunAndWait(Path.Combine(Directory.GetCurrentDirectory(), "StringDumping", "Last Call BBS.exe"), "");
			Console.WriteLine();
		}

		static void HandleIntermediary() {
			// TODO: MonoMod relinking?
			Console.WriteLine("Generating intermediary EXE...");
			LoadLightning();
			LoadStrings();
			// take Lightning.exe, remap to Intermediary
			CollectIntermediary();
			List<(Instruction, int)> stringsToBeInlined = new List<(Instruction, int)>();
			DoRemap(GetIntermediaryForName, Intermediary.ContainsKey, CollectNestedTypes(LightningAssembly.MainModule.Types),
				(mref, instr) => {
					if(mref.Name.Equals(StringDeobfIntermediaryName) && mref.Parameters.Count == 1)
						if(instr.Previous.OpCode == OpCodes.Ldc_I4)
							stringsToBeInlined.Add((instr, (int)instr.Previous.Operand));
				},
				type => {
					
					if(type.IsNested)
						type.IsNestedPublic = true;
					else
						type.IsPublic = true;

				});
			if(stringsToBeInlined.Count > 0)
				foreach(var stringFunc in stringsToBeInlined)
					if(Strings.ContainsKey(stringFunc.Item2)) {
						stringFunc.Item1.Previous.Set(OpCodes.Nop, null);
						stringFunc.Item1.Set(OpCodes.Ldstr, Strings[stringFunc.Item2]);
					} else
						Console.WriteLine($"Missing string for {stringFunc.Item2}");

			LightningAssembly.Write("IntermediaryMilwaukee.exe");
			Console.WriteLine();
		}

		static void LoadLightning() {
			Console.WriteLine("Reading Last Call BBS.exe...");
			LightningAssembly = AssemblyDefinition.ReadAssembly(PathToLightning);
			Console.WriteLine(LightningAssembly == null ? "Failed to load Last Call BBS.exe" : "Found Last Call BBS executable: " + LightningAssembly.FullName);
		}

		static void LoadModdedLightning() {
			Console.WriteLine("Reading modded Last Call BBS.exe...");
			ModdedLightningAssembly = AssemblyDefinition.ReadAssembly(PathToModdedLightning);
			Console.WriteLine(ModdedLightningAssembly == null ? $"Failed to load modded Last Call BBS.exe at \"{PathToModdedLightning}\"" : "Found modded Last Call BBS executable: " + ModdedLightningAssembly.FullName);
		}

		static void LoadStrings() {
			if(StringsPaths.Count > 0) {
				foreach(var path in StringsPaths) {
					if(!File.Exists(path))
						continue;
					string[] lines = File.ReadAllLines(path);
					int lastIndex = 0;
					foreach(string line in lines) {
						string[] split = line.Split(new string[] { "~,~" }, StringSplitOptions.None);
						if(split.Length > 1) {
							// if we *can* split on this line, then we're definitely at the first line of a string
							try {
								lastIndex = int.Parse(split[0]);
								Strings[lastIndex] = split[1];
							} catch(FormatException) { }
						} else {
							// if this line isn't blank (or even if it is), then we're continuing a previous multi-line string, so append
							Strings[lastIndex] = Strings[lastIndex] + "\n" + line;
						}
					}
				}
				Console.WriteLine("Loaded " + Strings.Count + " strings.");
			}
		}

		public static void DoRemap(Func<string, TypeDefinition, string> remapper, Func<string, bool> remapChecker, Collection<TypeDefinition> types, Action<MethodReference, Instruction> onMethodReference, Action<TypeDefinition> onTypeDefinition) {
			foreach(var type in types) {
				type.Name = remapper(type.Name, type);
				onTypeDefinition(type);
				foreach(var method in type.Methods) {
					// rtspecialname is applied to constructors and operators
					if(!method.IsRuntimeSpecialName)
						method.Name = remapper(method.Name, type);
					foreach(var param in method.Parameters)
						param.Name = remapper(param.Name, type);
					// references to members in classes with generic parameters don't get remapped automatically
					// so here we update those references ourself
					if(method.Body != null && method.Body.Instructions != null) {
						foreach(var instr in method.Body.Instructions) {
							if(instr != null && instr.Operand is MethodReference mref && !mref.IsWindowsRuntimeProjection) {
								if(remapChecker(mref.Name)) {
									if(mref.IsGenericInstance)
										mref = ((GenericInstanceMethod)mref).GetElementMethod();
									mref.Name = remapper(mref.Name, type);
								}
								// also take the oppurtunity to replace references to string decoder with the actual string
								onMethodReference(mref, instr);
							}

							if(instr != null && instr.Operand is FieldReference fref && remapChecker(fref.Name))
								fref.Name = remapper(fref.Name, type);
						}
					}
					foreach(var attr in method.CustomAttributes)
						if(attr.HasConstructorArguments)
							foreach(var arg in attr.ConstructorArguments)
								if(arg.Type.Name.Equals("Type"))
									(arg.Value as TypeReference).Name = remapper((arg.Value as TypeReference).Name, type);
					// TODO: map locals
				}
				foreach(var field in type.Fields)
					field.Name = remapper(field.Name, type);
				foreach(var generic in type.GenericParameters)
					generic.Name = remapper(generic.Name, type);
			}
		}

		static Collection<TypeDefinition> CollectNestedTypes(Collection<TypeDefinition> topLevel) {
			var types = new Collection<TypeDefinition>();
			foreach(var type in topLevel)
				VisitTypes(type, t => types.Add(t));
			return types;
		}

		static void CollectIntermediary() {
			// if a name is a valid CSharp name, it is its own intermediary
			// parse preset intermediary?

			// or gen intermediary
			int classIndex = 0, enumIndex = 0, interfaceIndex = 0, methodIndex = 0, structIndex = 0, delegateIndex = 0, fieldIndex = 0, genericIndex = 0, paramIndex = 0;
			foreach(var type in CollectNestedTypes(LightningAssembly.MainModule.Types)) {
				if(!Intermediary.ContainsKey(type.Name) && !type.IsRuntimeSpecialName) {
					// its a delegate if it descends from System.MulticastDelegate
					if(type.BaseType?.FullName?.Equals("System.MulticastDelegate") ?? false) {
						Intermediary.Add(type.Name, "delegate_" + delegateIndex);
						delegateIndex++;
					} else if(type.IsInterface) {
						Intermediary.Add(type.Name, "interface_" + interfaceIndex);
						interfaceIndex++;
					} else if(type.IsEnum) {
						Intermediary.Add(type.Name, "enum_" + enumIndex);
						enumIndex++;
					} else if(type.IsValueType) {
						Intermediary.Add(type.Name, "struct_" + structIndex);
						structIndex++;
					} else {
						Intermediary.Add(type.Name, "class_" + classIndex);
						classIndex++;
					}
				}
				foreach(var method in type.Methods) {
					if(!Intermediary.ContainsKey(method.Name) && !method.IsRuntimeSpecialName) {
						Intermediary.Add(method.Name, "method_" + methodIndex);
						methodIndex++;
					}
					foreach(var param in method.Parameters) {
						if(!Intermediary.ContainsKey(param.Name)) {
							Intermediary.Add(param.Name, "param_" + paramIndex);
							paramIndex++;
						}
					}
					// TODO: map locals
				}
				foreach(var field in type.Fields) {
					if(!Intermediary.ContainsKey(field.Name) && !field.IsRuntimeSpecialName) {
						Intermediary.Add(field.Name, "field_" + fieldIndex);
						fieldIndex++;
					}
				}
				foreach(var generic in type.GenericParameters) {
					if(!Intermediary.ContainsKey(generic.Name)) {
						Intermediary.Add(generic.Name, "generic_" + genericIndex);
						genericIndex++;
					}
				}
			}
		}

		static string GetIntermediaryForName(string name, TypeDefinition owner) {
			// if its already valid or its not in intermediary, leave it
			if(!Intermediary.ContainsKey(name) || Regex.Match(name, "^[a-zA-Z_\\`][a-zA-Z0-9_\\`]*$").Success)
				return name;
			// On Linux, changing <Module> to class_0 made Qml entirely nonfunctional.
			if(Intermediary[name] == "class_0") {
				return name;
			}
			// return intermediary
			return Intermediary[name];
		}

		static void VisitTypes(TypeDefinition top, Action<TypeDefinition> act) {
			act(top);
			foreach(var type in top.NestedTypes)
				VisitTypes(type, act);
		}

		static void HandleMerge() {
			// run "./MonoMod.exe IntermediaryLightning.exe Quintessential.dll ModdedLightning.exe"
			// then "./MonoMod.RuntimeDetour.HookGen.exe ModdedLightning.exe"
			if(File.Exists("./MonoMod.exe")) {
				if(File.Exists("./Quintessential.dll")) {
					// TODO: check if there's already quintessential with this version
					Console.WriteLine("Modding Lightning...");
					RunAndWait(Path.Combine(Directory.GetCurrentDirectory(), "MonoMod.exe"), "IntermediaryLightning.exe Quintessential.dll ModdedLightning.exe");
					if(!File.Exists("./ModdedLightning.exe")) {
						Console.WriteLine("Failed to mod!");
						return;
					}
					if(File.Exists("./MonoMod.RuntimeDetour.HookGen.exe")) {
						Console.WriteLine("Generating hooks...");
						RunAndWait(Path.Combine(Directory.GetCurrentDirectory(), "MonoMod.RuntimeDetour.HookGen.exe"), "ModdedLightning.exe");
						if(OpSystem != OS.Windows) {
							// Fixes the SDL2.dll not found error
							File.Copy("Lightning.exe.config", "ModdedLightning.exe.config", true);
							// These are the files you run to make the thing do the thing. yes
							File.Copy("Lightning.bin.x86", "ModdedLightning.bin.x86", true);
							File.Copy("Lightning.bin.x86_64", "ModdedLightning.bin.x86_64", true);
						}
					}
				} else {
					Console.WriteLine("Quintessential not found, skipping merging.");
				}
			} else {
				Console.WriteLine("MonoMod not found, skipping merging.");
			}
			Console.WriteLine();
		}

		static void HandleDevExe() {
			// take ModdedLightning.exe, remap to named
			Console.WriteLine("Generating dev EXE...");
			LoadModdedLightning();
			LoadMappings();
			DoRemap(GetNamedForIntermediary, Mappings.ContainsKey, CollectNestedTypes(ModdedLightningAssembly.MainModule.Types), (mref, instr) => { }, typeDef => { });
			ModdedLightningAssembly.Write("Last Call BBS.dev.exe");
			Console.WriteLine();
		}

		static void RunAndWait(string file, string param){
			Console.WriteLine("Running " + file);
			if(!File.Exists(file)) {
				Console.WriteLine("Failed to run " + file + ", file not found.");
				return;
			}
			Process process = new Process();
			// I'm unsure if Windows needs the file to have quotes
			// Just in case I'm leaving that in
			if(OpSystem == OS.Windows) {
				file = "\"" + file + "\"";
			}
			process.StartInfo.FileName = file;
			process.StartInfo.Arguments = param;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			process.WaitForExit();
			Console.WriteLine();
			Console.WriteLine("Process output:");
			Console.WriteLine(process.StandardOutput.ReadToEnd());
			Console.WriteLine();
		}

		static string GetNamedForIntermediary(string intermediary, TypeDefinition owner) {
			if(!Mappings.ContainsKey(intermediary))
				return intermediary;
			string name = Mappings[intermediary];
			if(name.Contains(".")) {
				string[] split = name.Split('.');
				name = split[split.Length - 1];
			}

			return name;
		}

		static void LoadMappings() {
			foreach(var path in MappingPaths) {
				if(!File.Exists(path))
					continue;
				string[] lines = File.ReadAllLines(path);
				foreach(var line in lines) {
					if(string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
						continue;
					if(!line.Contains(","))
						Console.WriteLine($"Invalid line in {path}: \"{line}\", missing comma.");
					string[] parts = line.Split(',');
					Mappings[parts[0]] = parts[1];
				}
			}
		}

		enum ArgumentParsingMode{
			Argument, MappingPath, IntermediaryPath, StringsPath, LightningPath, MonoModPath,
			MainMethodName, StringDeobfName, StringDeobfIntermediaryName
		}

		enum RunAction{
			Run, Strings, Intermediary, Merge, Setup, DevExe
		}
	}
}
