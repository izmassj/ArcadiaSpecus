using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class StringStringDictionary : SerializableDictionary<string, string> {}

[Serializable]
public class ObjectColorDictionary : SerializableDictionary<UnityEngine.Object, Color> {}

[Serializable]
public class ColorArrayStorage : SerializableDictionary.Storage<Color[]> {}

[Serializable]
public class StringColorArrayDictionary : SerializableDictionary<string, Color[], ColorArrayStorage> {}

[Serializable]
public class MachineKindGameObjectDictionary : SerializableDictionary<MachineKind, GameObject> { }

[Serializable]
public class RoomKindGameObjectDictionary : SerializableDictionary<RoomKind, GameObject> { }

[Serializable]
public class CornerTypeBoolDictionary : SerializableDictionary<CornerType, bool> { }

[Serializable]
public class CornerTypeCornerTypeDictionary : SerializableDictionary<CornerType, CornerType> { }

[Serializable]
public class MyClass
{
    public int i;
    public string str;
}

[Serializable]
public class QuaternionMyClassDictionary : SerializableDictionary<Quaternion, MyClass> {}