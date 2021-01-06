using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpritesheetImporter {

    public class ShowWhenAttribute : PropertyAttribute {

        public string OtherPropertyPath;

        public object OtherPropertyValue;

        public object[] OtherPropertyValues;
    }
}