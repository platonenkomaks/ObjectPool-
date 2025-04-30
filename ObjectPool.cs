using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простая система пула объектов для Unity.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private bool expandWhenNeeded = true;
    
    private List<GameObject> pooledObjects;
    
    /// <summary>
    /// Инициализация пула при запуске.
    /// </summary>
    private void Awake()
    {
        pooledObjects = new List<GameObject>();
        
        // Создаем начальный пул объектов
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewObject();
        }
    }
    
    /// <summary>
    /// Создает новый объект в пуле.
    /// </summary>
    private GameObject CreateNewObject()
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        obj.transform.SetParent(transform); // Делаем объект дочерним для пула
        
        // Если у объекта есть компонент PooledObject, устанавливаем ссылку на пул
        PooledObject pooledComponent = obj.GetComponent<PooledObject>();
        if (pooledComponent == null)
        {
            pooledComponent = obj.AddComponent<PooledObject>();
        }
        pooledComponent.SetPool(this);
        
        pooledObjects.Add(obj);
        return obj;
    }
    
    /// <summary>
    /// Получить объект из пула.
    /// </summary>
    public GameObject GetObject()
    {
        // Ищем неактивный объект
        foreach (GameObject obj in pooledObjects)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                
                // Вызываем OnGet для компонента PooledObject
                PooledObject pooled = obj.GetComponent<PooledObject>();
                if (pooled != null)
                {
                    pooled.OnGet();
                }
                
                return obj;
            }
        }
        
        // Если все объекты активны и разрешено расширение, создаем новый
        if (expandWhenNeeded)
        {
            GameObject newObj = CreateNewObject();
            newObj.SetActive(true);
            
            // Вызываем OnGet для компонента PooledObject
            PooledObject pooled = newObj.GetComponent<PooledObject>();
            if (pooled != null)
            {
                pooled.OnGet();
            }
            
            return newObj;
        }
        
        // Если все объекты используются и расширение не разрешено
        Debug.LogWarning("Pool is out of objects and expandWhenNeeded is false!");
        return null;
    }
    
    /// <summary>
    /// Получить объект из пула и установить его позицию и поворот.
    /// </summary>
    public GameObject GetObject(Vector3 position, Quaternion rotation)
    {
        GameObject obj = GetObject();
        
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        
        return obj;
    }
    
    /// <summary>
    /// Вернуть объект в пул.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        // Вызываем OnReturn для компонента PooledObject
        PooledObject pooled = obj.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.OnReturn();
        }
        
        obj.SetActive(false);
    }
    
    /// <summary>
    /// Вернуть объект в пул через указанное время.
    /// </summary>
    public void ReturnToPoolDelayed(GameObject obj, float delay)
    {
        StartCoroutine(ReturnToPoolAfterDelay(obj, delay));
    }
    
    /// <summary>
    /// Корутина для отложенного возврата объекта в пул.
    /// </summary>
    private IEnumerator ReturnToPoolAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(obj);
    }
}

/// <summary>
/// Компонент для объектов, используемых в пуле.
/// </summary>
public class PooledObject : MonoBehaviour
{
    private ObjectPool pool;
    
    /// <summary>
    /// Установить пул для этого объекта.
    /// </summary>
    public void SetPool(ObjectPool objectPool)
    {
        pool = objectPool;
    }
    
    /// <summary>
    /// Вызывается при получении объекта из пула.
    /// </summary>
    public virtual void OnGet()
    {
        // Переопределите этот метод в наследниках для настройки объекта при получении из пула
    }
    
    /// <summary>
    /// Вызывается при возврате объекта в пул.
    /// </summary>
    public virtual void OnReturn()
    {
        // Переопределите этот метод в наследниках для очистки состояния объекта
    }
    
    /// <summary>
    /// Вернуть этот объект в пул.
    /// </summary>
    public void ReturnToPool()
    {
        if (pool != null)
        {
            pool.ReturnToPool(gameObject);
        }
        else
        {
            Debug.LogWarning("Object has no pool assigned!");
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Вернуть объект в пул через указанное время.
    /// </summary>
    public void ReturnToPoolDelayed(float delay)
    {
        if (pool != null)
        {
            pool.ReturnToPoolDelayed(gameObject, delay);
        }
        else
        {
            Debug.LogWarning("Object has no pool assigned!");
            StartCoroutine(DeactivateAfterDelay(delay));
        }
    }
    
    /// <summary>
    /// Корутина для отложенной деактивации объекта.
    /// </summary>
    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}

/// <summary>
/// Пример реализации для снаряда (пули).
/// </summary>
public class Bullet : PooledObject
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private bool usePhysics = true;
    
    private Rigidbody rb;
    private TrailRenderer trail;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
    }
    
    /// <summary>
    /// Запустить пулю в указанном направлении.
    /// </summary>
    public void Launch(Vector3 direction)
    {
        if (usePhysics && rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }
        else
        {
            // Если нет Rigidbody, используем простое движение через Transform
            StartCoroutine(MoveCoroutine(direction));
        }
        
        ReturnToPoolDelayed(lifetime);
    }
    
    /// <summary>
    /// Корутина для движения пули без физики.
    /// </summary>
    private IEnumerator MoveCoroutine(Vector3 direction)
    {
        float timeAlive = 0;
        
        while (timeAlive < lifetime && gameObject.activeInHierarchy)
        {
            transform.position += direction.normalized * speed * Time.deltaTime;
            timeAlive += Time.deltaTime;
            yield return null;
        }
    }
    
    /// <summary>
    /// Вызывается при получении пули из пула.
    /// </summary>
    public override void OnGet()
    {
        base.OnGet();
        
        // Очищаем след, если он есть
        if (trail != null)
        {
            trail.Clear();
        }
    }
    
    /// <summary>
    /// Вызывается при возврате пули в пул.
    /// </summary>
    public override void OnReturn()
    {
        base.OnReturn();
        
        // Сбрасываем физику
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Останавливаем все корутины
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Обработка столкновений.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Возвращаем пулю в пул при столкновении
        ReturnToPool();
    }
}

/// <summary>
/// Пример простого стреляющего оружия, использующего пул объектов.
/// </summary>
public class SimpleGun : MonoBehaviour
{
    [SerializeField] private ObjectPool bulletPool;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.1f;
    
    private float nextFireTime;
    
    /// <summary>
    /// Выстрел из оружия.
    /// </summary>
    public void Fire()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }
        
        if (bulletPool != null && firePoint != null)
        {
            // Получаем пулю из пула
            GameObject bulletObj = bulletPool.GetObject(firePoint.position, firePoint.rotation);
            
            if (bulletObj != null)
            {
                // Запускаем пулю
                Bullet bullet = bulletObj.GetComponent<Bullet>();
                if (bullet != null)
                {
                    bullet.Launch(firePoint.forward);
                }
            }
            
            nextFireTime = Time.time + fireRate;
        }
        else
        {
            Debug.LogError("BulletPool or FirePoint not assigned!");
        }
    }
    
    /// <summary>
    /// Пример использования в Update.
    /// </summary>
    private void Update()
    {
        // Стреляем при нажатии на левую кнопку мыши
        if (Input.GetMouseButton(0))
        {
            Fire();
        }
    }
}
