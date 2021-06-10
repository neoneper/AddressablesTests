using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Protogame
{
    /// <summary>
    /// Este exemplo mostra como verificar todos os locais validos em uma combinacao de rotulos.
    /// Uma vez sabendo os enderecos validos, o teste libera a funcao <see cref="CreateRandomInstance"/>, para que instancias possam ser carregadas e criadas manualmente pelo usuario.  Tecla (A).
    /// Estas instancias sao criadas randomicamente de acordo com a lista de locais retornados pela pesquisa anteriormente feita. 
    ///
    /// Tambem e possivel destruir e limpar da memoria, manualmente, todas as instancias criadas ate o momento. Tecla (C).
    ///
    /// FLUXO DE TRABALHO:
    /// - Carregar todos os locais de uma ou mais label(s), que contenham as combinacoes exatas. <see cref="Addressables.LoadResourceLocationsAsync(IList{object}, Addressables.MergeMode, Type)"/>
    /// - Armazenar na lista <see cref="locationsList"/>, todos os enderecos <see cref="IResourceLocation"/> encontrados.
    /// Esta lista sera pesquisada sempre que uma nova instancia for requisitada <see cref="CreateRandomInstance"/>>.
    /// Observe que o IResourceLocation armazenado nao carrega o a referencia na memoria, tal como feito com <see cref="Addressables.LoadAssetAsync{TObject}(IResourceLocation)"/>.
    /// O Local serve apenas para saber os enderecos, existentes, para que possamos carregar ou instanciar futuramente.
  	/// Limpar memoria da pesquisa.
  	/// Permitir Criacao ou Remocao de instancias manualmente Teclas: (A)= criar, (C)= apagar tudo.
    /// - Armazenar no dicionario <see cref="instances"/> o local e a operacao de instancia, para que possamos desalocar manulamente futuramente quando Limpeza for requisitado:Tecla (C)
    /// </summary>
    public class AssetsRef_LocationsLoader_Test : MonoBehaviour
    {
        /// <summary>
        /// Lista com rotulos, utilizada para pesquisar locais que contenham exatamente todas as labels designadas.
        /// Modifique para as suas necessidades!
        /// </summary>
        public List<string> labels = new List<string>() { "weapon", "prefab" };

        /// <summary>
        /// Operacao utilizada para carregar a lista de locais. Esta operaracao e liberada da memoria logo appos popular a lista <see cref="locationsList"/>.
        /// </summary>
        private AsyncOperationHandle<IList<IResourceLocation>> locationOperationHandle;

        /// <summary>
        /// Esta lista contem o endereco de todos os locais validos para os rotulos <see cref="labels"/>.
        /// Uitilize esta lista para carregar e instanciar os objetos destes rotulos.
        /// Nota:
        /// Nao utilize o resultado de <see cref="locationOperationHandle"/>, para pesquisar por locais, pois ele e limpo da memoria logo apos popular esta lista!.
        /// </summary>
        private List<IResourceLocation> locationsList = new List<IResourceLocation>();

        /// <summary>
        /// Armazena a instancia criada juntamente com a operacao que a criou. Isto e util para limpar da memoria a instancia quando o objeto necessitar ser destruido por Addressable.ReleaseInstance.
        ///
        /// Importante:
        /// Se o parametro trackHandle do InstantiateAsync for verdadeiro, nao existe a necessidade de fazer esta limpeza manualmente. Assim sendo este exeplo serve apenas para avaliacao de como seria
        /// manualmente, caso trackHandle seja setado como falso!
        /// </summary>
        private Dictionary<int, AsyncOperationHandle<GameObject>> instances = new Dictionary<int, AsyncOperationHandle<GameObject>>();


        /// <summary>
        /// Verdadeiro quando <see cref="LoadLocations"/> finalizar sua pesquisa!
        /// Usado apenas para permitir ou nao o uso dos <see cref="Input.GetKeyDown(KeyCode)"/>, utilizados para adicionar ou limpar instancias na funcao <see cref="Update"/>
        /// </summary>
        private bool isDone = false;

        private void OnEnable()
        {
            LoadLocations();
        }
        private void OnDisable()
        {
            isDone = false;

            UnLoadAllCreatedInstances();

            locationsList.Clear();


        }
        private void Update()
        {
            if (isDone == false)
                return;

            if (Input.GetKeyUp(KeyCode.A))
            {
                CreateRandomInstance();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                UnLoadAllCreatedInstances();
            }
        }

        private void LoadLocations()
        {
            locationOperationHandle = Addressables.LoadResourceLocationsAsync(labels, Addressables.MergeMode.Intersection);
            locationOperationHandle.Completed += OnLoadPrefabsCompleted;

        }

        public void UnLoadAllCreatedInstances()
        {
            foreach (var i in instances.Keys)
            {
                Addressables.ReleaseInstance(instances[i]);
            }

            instances.Clear();

            Debug.Log("Unload and destroyed all instances");
        }


        /// <summary>
        /// Cria uma nova instancia randomica, baseada nos resultados encontrados na lista de locais. E preciso ser asyncrono ou syncrono pois LoadResourceLocationsAsync nao carrega
        /// a referencia previamente para manter o Instantiate instantaneo. 
        /// Importante:
        /// Eu deveria usar trackHandle = false na instancia, pois eu quero garantir o desalocamento manual da referencia instanciada na memoria. Mas vou manter o default
        /// true pois eu nao entendo perfeitamente o funcionamento dele nas dependencias do objeto instanciado. Tambem nao tenho certeza se eu deixar false, o objeto sera limpo da memoria
        /// quando a cena for descarregada!.
        ///
        /// Imporatnte2: Tasks nao funcionam em webGL. Usar Corotinas para esta finalidade
        /// 
        /// </summary>
        /// <returns></returns>
        public async System.Threading.Tasks.Task CreateRandomInstance()
        {
            int min = 0;
            int max = locationsList.Count - 1;
            int random = UnityEngine.Random.Range(min, max);
            int index = random < 0 ? 0 : random;



            //A instancia foi setada para nao gerenciar o objeto criado pela operacao. Isto define que a limpeza de memoria deve ser feita manualmente por mim.
            //Por este motivo eu vou adicionar a operacao e o id da instancia em um dicionario, para poder limpar manualmente posteriormente.

            AsyncOperationHandle<GameObject> op = Addressables.InstantiateAsync(locationsList[index]); //Mantive o default por falta de exclarecimentos

            //A Instancia criada pela operacao nao e isntantanea, pois nao foi carregada por LoadAssetAsyn, e sim por LoadResourceLocationAsync.
            //Neste caso, existe a necessidade de esperar sync ou o callback de finalizacao de instancia com carregamento.
            //Particulamente eu acho que isto e um BUG do Addressable.
            await op.Task;

            if (op.IsDone)
            {

                GameObject instanceGo = op.Result;
                int instanceID = instanceGo.GetInstanceID();

                //Guardando no dicionario a operacao e a instancia. Vou usar isso para limpar a memoria posteriormente
                instances.Add(instanceID, op);

                Debug.Log("Instantied Ref: [" + instanceID + "] - " + op.Result.name);

            }

        }


        private void OnLoadPrefabsCompleted(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {

            foreach (IResourceLocation go in handle.Result)
            {

                locationsList.Add(go);
                Debug.Log("Loaded location: " + go.PrimaryKey);

            }


            locationOperationHandle.Completed -= OnLoadPrefabsCompleted;
            isDone = true;

            Addressables.Release(locationOperationHandle);

            Debug.Log("All locations loaded");
        }


    }
}
