using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class pLab_PointOfInterestManager : MonoBehaviour
{
    [SerializeField]
    private pLab_PointOfInterestSet pointOfInterestSet; // Scriptable Object referansý

    [SerializeField]
    private Button addButton; // Buton referansý

    [Header("GPS-AR Mode")]
    [SerializeField]
    private pLab_LocationProvider locationProvider;

    private bool isFakeLocation = false; // Sahte veri kontrolü için bir flag

    private void Start()
    {
        // Butonun týklandýðýnda AddPointOfInterest fonksiyonunu çaðýr
        addButton.onClick.AddListener(AddPointOfInterest);

      
    }

    private void AddPointOfInterest()
    {
        // locationProvider'ýn null olup olmadýðýný kontrol et
        if (locationProvider == null)
        {
            Debug.LogError("Location Provider atanmadý. Lütfen editor'den atayýn.");
            return; // Location Provider yoksa iþlemi durdur
        }

        // Yeni Point of Interest nesnesi oluþtur
        pLab_PointOfInterest newPointOfInterest = ScriptableObject.CreateInstance<pLab_PointOfInterest>();

        // Burada yeni nesneye deðerler atayabilirsiniz
        newPointOfInterest.PoiName = "Yeni POI"; // Örnek isim
        newPointOfInterest.Description = "Bu yeni bir POI açýklamasýdýr."; // Örnek açýklama

        // locationProvider'dan enlem ve boylam deðerlerini al
        if (locationProvider.Location != null) // Eðer gerçek veya sahte konum bilgisi mevcutsa
        {
            if (isFakeLocation)
            {
                // Sahte koordinatlarý al
                newPointOfInterest.Coordinates = locationProvider.FakeCoordinates;
                Debug.LogWarning("Sahte GPS verisi kullanýlýyor! Sahte koordinatlar alýndý.");
            }
            else
            {
                // Gerçek koordinatlarý al
                newPointOfInterest.Coordinates = locationProvider.Location;
            }
        }
      

        // Benzersiz bir isim oluþtur
        string uniqueId = System.Guid.NewGuid().ToString();
        string assetPath = $"Assets/Examples/ScriptableObjects/Point of Interests/New Point of Interest {uniqueId}.asset";

#if UNITY_EDITOR
        // Asset olarak belirtilen dizine kaydet (Sadece editör modunda çalýþýr)
        AssetDatabase.CreateAsset(newPointOfInterest, assetPath);
        AssetDatabase.SaveAssets();

        // Yeni POI'yi set'e ekle
        pointOfInterestSet.PointOfInterests.Add(newPointOfInterest);

        // Deðiþiklikleri kaydet
        EditorUtility.SetDirty(pointOfInterestSet);
#else
        Debug.LogWarning("AddPointOfInterest fonksiyonu sadece Unity Editor içinde çalýþýr.");
#endif
    }
}
