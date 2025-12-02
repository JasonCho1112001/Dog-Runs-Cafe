using UnityEngine;

public class menuManagerScript : MonoBehaviour
{
    
    public GameObject[] pages;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void OpenPage(int pageIndex)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            if (i == pageIndex)
            {
                pages[i].SetActive(true);
            }
            else
            {
                pages[i].SetActive(false);
            }
        }
    }

    public void NextPage(int currentPageIndex)
    {
        int nextPageIndex = (currentPageIndex + 1) % pages.Length;
        OpenPage(nextPageIndex);
    }
}
