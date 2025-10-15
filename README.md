Com certeza. Com base na nossa discussão e nas melhores práticas, preparei uma versão completa e profissional para o seu `README.md`.

Este modelo organiza as seções de forma lógica, separa claramente os ambientes de desenvolvimento (Docker Compose vs. Kubernetes), adiciona as etapas que estavam faltando (como a construção e importação de imagens para o k3d) e fornece explicações mais detalhadas para cada comando.

---

# Projeto LeadLists

Este repositório contém a implementação de um sistema completo para gerenciamento de "Listas de Leads". O projeto foi desenvolvido para demonstrar um fluxo de ponta a ponta, incluindo um frontend em React, uma API em .NET 9, e uma arquitetura de microsserviços com PostgreSQL, RabbitMQ e orquestração com Kubernetes (k3d).

## Arquitetura

O sistema utiliza um padrão de **polling** para desacoplar a criação de tarefas do seu processamento, garantindo resiliência e escalabilidade.

1. **Criação da Tarefa:** O Frontend envia uma requisição para a **API**, que cria um registro no **PostgreSQL** com o status `Pending`.
    
2. **Processamento em Background:** Um **Worker** contínuo sonda o banco de dados em intervalos regulares, procurando por tarefas `Pending`.
    
3. **Execução:** Ao encontrar uma tarefa, o Worker a executa, atualizando o status no banco para `Processing` e, ao final, para `Completed` ou `Failed`.
    
4. **Notificação de Falha:** Em caso de falha no processamento, o Worker publica uma mensagem em uma fila específica (`leadlist.failed`) no **RabbitMQ** para análise posterior ou para acionar processos de recuperação.
    

## Pré-requisitos

Antes de começar, garanta que você tenha as seguintes ferramentas instaladas e configuradas em seu ambiente:

#### Backend e Infraestrutura

- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
    
- [Docker e Docker Compose](https://docs.docker.com/get-docker/)
    
- [k3d](https://www.google.com/search?q=https://k3d.io/v5.6.0/%23install) (para o cluster Kubernetes local)
    
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
    
- (Opcional) [Lens](https://k8slens.dev/) ou [OpenLens](https://github.com/MuhammedKalkan/OpenLens) para uma visualização gráfica do cluster.
    

#### Frontend

- [Node.js 20+](https://nodejs.org/en/download)
    
- NPM ou Yarn
    

---

## Guia de Execução

Existem duas formas de rodar este projeto: localmente com Docker Compose (recomendado para desenvolvimento rápido) ou em um cluster Kubernetes com k3d (simulando um ambiente de produção).

### 1. Rodando Localmente com Docker Compose

Este modo é ideal para desenvolver e testar a API, o Worker e o Frontend de forma isolada.

#### Passo 1: Iniciar a Infraestrutura

O comando abaixo irá iniciar os contêineres do **Postgres** e **RabbitMQ** em background.

Bash

```
docker-compose up -d
```

#### Passo 2: Rodar a API

Em um novo terminal, navegue até a pasta da API, aplique as migrações do banco de dados e inicie a aplicação.

Bash

```
cd API/LeadListAPI

# Aplica as migrações do Entity Framework no banco de dados
dotnet ef database update

# Inicia a API com hot-reload
dotnet watch run
```

A documentação da API (Swagger UI) estará disponível em: `http://localhost:5267/swagger`

#### Passo 3: Rodar o Worker

O Worker é o serviço que processa as listas em background. Abra um novo terminal e inicie-o.

Bash

```
cd worker

# Inicia o Worker com hot-reload
dotnet watch run
```

#### Passo 4: Rodar o Frontend

Finalmente, em outro terminal, inicie a aplicação React.

Bash

```
cd frontend/lead-lists-frontend

# Instala as dependências (apenas na primeira vez)
npm install

# Inicia o servidor de desenvolvimento
npm run dev
```

Acesse a aplicação em: `http://localhost:5173`

---

### 2. Rodando no Kubernetes com k3d

Este modo simula um deploy completo em um ambiente de produção, orquestrando todos os serviços dentro de um cluster Kubernetes.

#### Passo 1: Build e Importação das Imagens

O k3d roda em seu próprio ambiente Docker e não tem acesso direto às imagens que você constrói localmente. Portanto, primeiro precisamos construir as imagens da API e do Worker e depois importá-las para o cluster.

Bash

```
# Construir a imagem da API
docker build -t leadlist-api:local ./API/LeadListAPI

# Construir a imagem do Worker
docker build -t leadlist-worker:local ./worker
```

#### Passo 2: Criar o Cluster k3d

Este comando cria o cluster e mapeia as portas da API e do RabbitMQ para o seu `localhost`.

Bash

```
k3d cluster create leadlists -p "8080:30080@loadbalancer" -p "15672:31567@loadbalancer"
```

#### Passo 3: Importar as Imagens para o Cluster

Agora, importe as imagens que você acabou de construir.

Bash

```
k3d image import leadlist-api:local -c leadlists
k3d image import leadlist-worker:local -c leadlists
```

#### Passo 4: Aplicar os Manifestos do Kubernetes

Com o cluster criado e as imagens importadas, aplique todos os manifestos de configuração de uma só vez.

Bash

```
# O comando -R aplica recursivamente todos os arquivos .yaml na pasta K8s
kubectl apply -f K8s/ -R
```

Este comando irá configurar toda a aplicação no namespace `dev`, incluindo:

- **PostgreSQL:** `StatefulSet`, `Service` e `Secret`.
    
- **RabbitMQ:** `Deployment`, `Services`, `Secret` e `ConfigMap`.
    
- **API:** `Deployment`, `Service` e `RBAC` (permissões).
    
- **Worker:** `Deployment`.
    

#### Passo 5: Verificação

Para verificar se todos os pods estão rodando corretamente:

Bash

```
kubectl get pods -n dev
```

Aguarde até que todos os pods estejam com o status `Running` e `1/1` na coluna `READY`.

Para acompanhar os logs de um serviço:

Bash

```
# Logs da API
kubectl logs -n dev -l app=api -f

# Logs do Worker
kubectl logs -n dev -l app=worker -f
```

---

### Testando o Fluxo de Ponta a Ponta

1. **Inicie o ambiente** (local ou k3d).
    
2. **Acesse o Frontend** (`http://localhost:5173`) ou a **API via Swagger** (`http://localhost:8080/swagger` para k3d, `http://localhost:5267/swagger` para local).
    
3. **Crie uma nova LeadList** através da interface ou enviando uma requisição `POST`.
    
4. **Observe o Status:** Acompanhe o status da lista, que deve transitar de `Pending` para `Processing` e, finalmente, para `Completed` ou `Failed`.
    
5. **Verifique a Fila de Falhas:** Em caso de falha, acesse a **UI do RabbitMQ** (`http://localhost:15672`, user/pass: `guest`) e verifique se uma mensagem apareceu na fila `leadlist.failed`.
    

### Gerenciamento do Cluster

Para destruir o cluster k3d e todos os seus recursos, execute:

Bash

```
k3d cluster delete leadlists
```