using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomPlugin
{
    public class MetaDataObject
    {
        public GameObject item;
        public MetaDataObject(GameObject item)
        {
            this.item = item;
        }

        public string name;
        public string type;
        public string coordinates;
        public GameObject parent;
        public List<GameObject> children;
    }
}
