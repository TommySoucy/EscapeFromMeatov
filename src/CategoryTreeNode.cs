using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class CategoryTreeNode
    {
        public CategoryTreeNode parent;
        public List<CategoryTreeNode> children;

        public string ID;
        public string name;

        public CategoryTreeNode(CategoryTreeNode parent, string ID, string name)
        {
            this.parent = parent;
            children = new List<CategoryTreeNode>();
            if(parent != null)
            {
                parent.children.Add(this);
            }

            this.ID = ID;
            this.name = name;
        }

        public CategoryTreeNode FindChild(string ID)
        {
            if (ID.Equals(this.ID))
            {
                return this;
            }
            else
            {
                for(int i=0; i < children.Count; ++i)
                {
                    CategoryTreeNode child = children[i].FindChild(ID);
                    if(child != null)
                    {
                        return child;
                    }
                }
            }

            return null;
        }
    }
}
