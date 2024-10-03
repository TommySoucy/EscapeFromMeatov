using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ProductionView : MonoBehaviour
    {
        public AreaUI area;
        public Production production;
        public Transform requirementsPanel;
        public GameObject requirementItemViewPrefab;
        public TimePanel timePanel;
        public ResultItemView resultItemView;
        public GameObject startButton;
        public GameObject getButton;
        public GameObject productionStatus;
        public Text productionStatusText;

        public void OnStartClicked()
        {
            production.BeginProduction();

            startButton.SetActive(false);
        }

        public void OnGetClicked()
        {
            production.ReturnTools();
            production.SpawnProduct();

            getButton.SetActive(production.readyCount > 0);

            production.areaData.area.UI.UpdateStatusIcons();
            production.areaData.area.UI.UpdateStatusTexts();
        }
    }
}
