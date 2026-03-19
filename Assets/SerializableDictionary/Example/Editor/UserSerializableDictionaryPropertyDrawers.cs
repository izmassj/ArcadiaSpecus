using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(StringStringDictionary))]
[CustomPropertyDrawer(typeof(ObjectColorDictionary))]
[CustomPropertyDrawer(typeof(StringColorArrayDictionary))]
public class AnySerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer {}

[CustomPropertyDrawer(typeof(ColorArrayStorage))]
public class AnySerializableDictionaryStoragePropertyDrawer: SerializableDictionaryStoragePropertyDrawer { }

[CustomPropertyDrawer(typeof(MachineKindGameObjectDictionary))]
public class MachineKindGameObjectDrawer : SerializableDictionaryPropertyDrawer { }

[CustomPropertyDrawer(typeof(RoomKindGameObjectDictionary))]
public class RoomKindGameObjectDrawer : SerializableDictionaryPropertyDrawer { }

[CustomPropertyDrawer(typeof(CornerTypeBoolDictionary))]
public class CornerTypeBoolDictionaryDrawer : SerializableDictionaryPropertyDrawer { }

[CustomPropertyDrawer(typeof(CornerTypeCornerTypeDictionary))]
public class CornerTypeCornerTypeDictionaryDrawer : SerializableDictionaryPropertyDrawer { }


