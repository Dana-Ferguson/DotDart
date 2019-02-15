### DotDart

Playing around with Dart Kernel in C#.
At the moment, we can load some version 12 binary DILL files.

```Dart
var dart = 'dart-lang';
main() {
  print('hello from $dart!');
}
```

becomes Kernel IR

```
ComponentFile:
    libraries:
        [0] Library:
            flags: 0
            canonicalName: CanonicalNameReference:
                biasedIndex: 1
                value: 'file:///home/dana/RiderProjects/DartAstTest/DotDart/test_scripts/hello.dart'
            name: (0) => ''
            fileUri: UriReference:
                index: 1
            annotations:
            libraryDependencies:
            additionalExports:
            libraryParts:
            typedefs:
            classes:
            fields:
                [0] Field:
                    canonicalName: CanonicalNameReference:
                        biasedIndex: 3
                        value: 'dart'
                    fileUri: UriReference:
                        index: 1
                    fileOffset: FileOffset: 4
                    fileEndOffset: FileOffset: 4294967295
                    flags: isStatic
                    name: Name:
                        name: (1) => 'dart'
                        library: NULL
                    annotations:
                    type: SimpleInterfaceType:
                        classReference: ClassReference:
                            canonicalName: CanonicalNameReference:
                                biasedIndex: 7
                                value: 'String'
                    initializer: Something<DotDart.Expression>
                        StringLiteral:
                            value: (2) => 'dart-lang'
            procedures:


procedures:
   [0] Procedure:
      canonicalName: CanonicalNameReference:
         biasedIndex: 5
         value: 'main'
      fileUri: UriReference:
         index: 1
      startFileOffset: FileOffset: 24
      fileOffset: FileOffset: 24
      fileEndOffset: FileOffset: 65
      kind: Method
      flags: isStatic
      name: Name:
         name: (3) => 'main'
         library: NULL
      annotations:
      forwardingStubSuperTarget: Nothing<DotDart.MemberReference>
      forwardingStubInterfaceTarget: Nothing<DotDart.MemberReference>
      function: Something<DotDart.FunctionNode>
         FunctionNode:
            fileOffset: FileOffset: 28
            fileEndOffset: FileOffset: 65
            asyncMarker: 0
            dartAsyncMarker: 0
            typeParameters:
            parameterCount: 0
            requiredParameterCount: 0
            positionalParameters:
            namedParameters:
            returnType: DynamicType
            body: Something<DotDart.Statement>
               Block: ***

Block:
   statements:
      [0] ExpressionStatement:
         expression: StaticInvocation:
            fileOffset: FileOffset: 35
            target: MemberReference:
               canonicalName: CanonicalNameReference:
                  biasedIndex: 9
                  value: 'print'
            arguments: Arguments:
               numArguments: 1
               types:
               positional:
                  [0] StringConcatenation:
                     fileOffset: FileOffset: 62
                     expressions:
                        [0] StringLiteral:
                           value: (4) => 'hello from '
                        [1] StaticGet:
                           fileOffset: FileOffset: 55
                           target: MemberReference:
                              canonicalName: CanonicalNameReference:
                                 biasedIndex: 3
                                 value: 'dart'
                        [2] StringLiteral:
                           value: (5) => '!'
               named:
```

which is translated into Roslyn AST, which produces

```c#
using System;
using static DotDart.DartCore;

static class file_home_dana_RiderProjects_DartAstTest_Tests_scripts_hello_dart
{
    static string dart = "dart-lang";
    static public dynamic main()
    {
        print($"hello from {dart}!");
        return default;
    }
}
```

which can be executed as a script to produce

```
hello from dart-lang!
```

This is what peak fun looks like.