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

            this.ID = ID;
            this.name = name;
        }

        public CategoryTreeNode FindChild(string ID)
        {
            foreach(CategoryTreeNode child in children)
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
