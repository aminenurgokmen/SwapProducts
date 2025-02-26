using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailSystemScript : MonoBehaviour
{
    public Transform productEnter; // Başlangıç noktası (pembe nokta)
    public Transform xPoint; // X noktası (arayol)
    public Transform yPoint; // Y noktası (Fixture'den çıkış için)
    public Transform prodExit; // Kırmızı ürünü yok edeceğimiz nokta

    public List<Transform> fixtureEnterPoints; // Fixture giriş noktaları
    public List<Transform> fixturePoints; // Fixture noktaları (ürün bırakılacak)

    public GameObject productPrefab; // Inspector'dan atanacak ürün prefabı
    private GameObject currentProduct; // RailSystem'in taşıdığı ürün
    private GameObject redProduct; // Fixture'dan alınan kırmızı ürün

    private int currentFixtureIndex = 0; // Şu an hangi fixture'ı ziyaret ettiğimizi takip eder
    private bool isSwapPhase = false; // Swap sürecinin başlayıp başlamadığını belirler

    private void Start()
    {
        StartCoroutine(SpawnAndPickProduct());
    }

    IEnumerator SpawnAndPickProduct()
    {
        // Eğer swap süreci başlamadıysa ürün spawnla
        if (!isSwapPhase)
        {
            SpawnProduct(productEnter.position);
        }

        // RailSystem ürünü alsın ve çocuğu yapsın
        yield return new WaitForSeconds(.1f);
        currentProduct.transform.parent = transform;
        Debug.Log("Ürün alındı ve RailSystem'e bağlandı!");

        // X noktasına git
        yield return MoveTo(xPoint.position);

        // Eğer swap süreci başlamadıysa ürün bırakma süreci devam etsin
        if (!isSwapPhase)
        {
            yield return StartCoroutine(ProcessNextFixture());
        }
        else
        {
            yield return StartCoroutine(ProcessNextSwap());
        }
    }

    IEnumerator ProcessNextFixture()
    {
        if (currentFixtureIndex >= fixtureEnterPoints.Count)
        {
            Debug.Log("Tüm fixture noktaları doldu. Şimdi swap sürecine geçiliyor.");
            isSwapPhase = true; // Swap sürecini başlat
            currentFixtureIndex = 0; // Swap için tekrar sıfırdan başla
            StartCoroutine(ProcessNextSwap()); // Swap başlat
            yield break;
        }

        // FixtureEnter noktasına git
        yield return MoveTo(fixtureEnterPoints[currentFixtureIndex].position);

        // Fixture kontrolü yap ve ürün bırak
        FixtureScript fixtureScript = fixturePoints[currentFixtureIndex].GetComponent<FixtureScript>();
        if (fixtureScript != null && !fixtureScript.doluMu)
        {
            yield return MoveTo(fixturePoints[currentFixtureIndex].position);
            DropProduct(fixtureScript);
        }

        // FixtureEnter noktasına geri dön
        yield return MoveTo(fixtureEnterPoints[currentFixtureIndex].position);

        // X noktasına geri dön
        yield return MoveTo(xPoint.position);

        // ProdEnter noktasına dön ve yeni ürün al
        yield return MoveTo(productEnter.position);

        // Bir sonraki fixture için devam et
        currentFixtureIndex++;
        StartCoroutine(SpawnAndPickProduct());
    }

    IEnumerator ProcessNextSwap()
    {
        if (currentFixtureIndex >= fixtureEnterPoints.Count)
        {
            Debug.Log("Tüm fixture swap işlemleri tamamlandı. Başa dönüyoruz.");
            yield return StartCoroutine(ReturnToFirstFixture()); // Döngüyü baştan başlat
            yield break;
        }

        // X noktasına git
        yield return MoveTo(xPoint.position);

        // FixtureEnter_X noktasına git
        yield return MoveTo(fixtureEnterPoints[currentFixtureIndex].position);

        // Fixture_X noktasına git ve swap işlemi yap
        yield return MoveTo(fixturePoints[currentFixtureIndex].position);
        SwapProduct(fixturePoints[currentFixtureIndex].GetComponent<FixtureScript>());

        // FixtureEnter_X noktasına geri dön
        yield return MoveTo(fixtureEnterPoints[currentFixtureIndex].position);

        // Y noktasına git
        yield return MoveTo(yPoint.position);

        // ProdExit noktasına git ve kırmızı ürünü yok et
        yield return MoveTo(prodExit.position);
        DestroyRedProduct();

        // ProdEnter noktasına git ve yeni ürün al
        yield return MoveTo(productEnter.position);
        SpawnProduct(productEnter.position);
        currentProduct.transform.parent = transform;
        Debug.Log("Yeni ürün alındı, swap işlemi devam ediyor.");

        // **Döngüsel olarak Fixture_0’a geri dönecek**
        currentFixtureIndex++;
        if (currentFixtureIndex >= fixtureEnterPoints.Count)
        {
            currentFixtureIndex = 0; // **Fixture_0'dan tekrar başla**
        }

        StartCoroutine(ProcessNextSwap());
    }


    IEnumerator ReturnToFirstFixture()
    {
        Debug.Log("Fixture_0'a dönüş başlıyor.");

        // X noktasına git
        yield return MoveTo(xPoint.position);

        // FixtureEnter_0 noktasına git
        yield return MoveTo(fixtureEnterPoints[0].position);

        // Fixture_0 noktasına git ve swap işlemi yap
        yield return MoveTo(fixturePoints[0].position);
        SwapProduct(fixturePoints[0].GetComponent<FixtureScript>());

        // FixtureEnter_0'a geri dön
        yield return MoveTo(fixtureEnterPoints[0].position);

        // Y noktasına git
        yield return MoveTo(yPoint.position);

        // ProdExit noktasına git ve kırmızı ürünü yok et
        yield return MoveTo(prodExit.position);
        DestroyRedProduct();

        // ProdEnter noktasına git ve yeni ürün al
        yield return MoveTo(productEnter.position);
        SpawnProduct(productEnter.position);
        currentProduct.transform.parent = transform;
        Debug.Log("Yeni ürün alındı, yeni swap döngüsü başlatılıyor.");

        // **Fixture_1’den itibaren tekrar swap işlemi başlat**
        currentFixtureIndex = 1;
        StartCoroutine(ProcessNextSwap());
    }




    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * 15);
            yield return null;
        }
        yield return new WaitForSeconds(.1f);
    }

    void SpawnProduct(Vector3 spawnPosition)
    {
        if (productPrefab != null)
        {
            currentProduct = Instantiate(productPrefab, spawnPosition, Quaternion.identity);
            currentProduct.transform.parent = transform; // RailSystem'in çocuğu yap
            currentProduct.transform.localPosition = new Vector3(-0.8f, 1f, 0f); // **Local pozisyonu ayarla**
            Debug.Log("Yeni ürün spawn edildi ve RailSystem’e bağlandı!");
        }
        else
        {
            Debug.LogWarning("Product Prefab atanmadı! Lütfen Inspector'dan ekleyin.");
        }
    }


    void DropProduct(FixtureScript fixtureScript)
    {
        if (currentProduct != null)
        {
            currentProduct.transform.parent = fixtureScript.transform;
            currentProduct.transform.localPosition = Vector3.zero;
            fixtureScript.doluMu = true;
            Debug.Log($"Ürün bırakıldı ve {fixtureScript.gameObject.name} çocuğu oldu.");
        }
    }
    void SwapProduct(FixtureScript fixtureScript)
    {
        if (fixtureScript != null && fixtureScript.doluMu)
        {
            Debug.Log("Swap işlemi başlatılıyor...");

            // Mevcut ürünü bırak, fixture'ın çocuğu yap
            if (currentProduct != null)
            {
                currentProduct.transform.parent = fixtureScript.transform;
                currentProduct.transform.localPosition = Vector3.zero; // **Fixture'in merkezine bırak**
                Debug.Log("Mevcut ürün Fixture'e bırakıldı.");
            }
            else
            {
                Debug.LogWarning("RailSystem'de ürün yok! Swap işlemi hatalı olabilir.");
            }

            // Fixture içindeki ürünü al, RailSystem'in çocuğu yap
            if (fixtureScript.transform.childCount > 0)
            {
                Transform fixtureProduct = fixtureScript.transform.GetChild(0);
                fixtureProduct.parent = transform; // **RailSystem'in çocuğu yap**
                fixtureProduct.localPosition = new Vector3(0.8f, 1f, 0f); // **RailSystem içinde yeni pozisyon**
                fixtureProduct.GetComponent<Renderer>().material.color = Color.red; // **Kırmızıya boya**
                redProduct = fixtureProduct.gameObject;
                Debug.Log("Fixture'daki ürün RailSystem’e alındı, kırmızıya boyandı ve doğru konumda.");
            }
            else
            {
                Debug.LogWarning("Fixture’da alınacak ürün bulunamadı! Swap işlemi eksik olabilir.");
            }
        }
    }



    void DestroyRedProduct()
    {
        if (redProduct != null)
        {
            Destroy(redProduct);
            redProduct = null;
            Debug.Log("Kırmızı ürün yok edildi.");
        }
        else
        {
            Debug.LogWarning("Yok edilecek kırmızı ürün bulunamadı.");
        }
    }

}
