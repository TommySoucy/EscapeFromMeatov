using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class EFM_CategoryTreeNode
    {
        public EFM_CategoryTreeNode parent;
        public List<EFM_CategoryTreeNode> children;

        public string ID;
        public string name;

        public EFM_CategoryTreeNode(EFM_CategoryTreeNode parent, string ID, string name)
        {
            this.parent = parent;
            children = new List<EFM_CategoryTreeNode>();

            this.ID = ID;
            this.name = name;
        }

        public EFM_CategoryTreeNode FindChild(string ID)
        {
            foreach(EFM_CategoryTreeNode child in children)
            {
                if (child.ID.Equals(ID))
                {
                    return child;
                }
            }
            return null;
        }
    }
}
