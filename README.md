# Fogueira — Unity 6

Projeto de demonstração de uma fogueira com **chamas, fumaça, faíscas, brasas, lenha e luz tremulante**.

## Como abrir
1. Clone este repositório e abra a **pasta raiz** no Unity Hub com Unity **6.0**.
2. Aguarde a importação do Universal Render Pipeline (URP) e Visual Effect Graph.
3. O instalador em `Assets/Editor/InstalarFogueira.cs` gera automaticamente `Assets/Scenes/Fogueira.unity` na primeira abertura do Editor.
4. Abra a cena e pressione **Play**, na aba **Game**.
5. Se a cena não surgir, use **Ferramentas > Fogueira > Recriar cena**.

## Conteúdo
- Fogo, fumaça, faíscas e brasa: partículas visíveis em Game para demonstração.
- Lenha, chão, pedras, câmera, iluminação com oscilação.
- Um componente **Visual Effect** (GPU) recebe uma cópia do modelo **Simple Loop** do pacote VFX Graph em `Assets/VFX/Fogueira_GPU.vfx`.

## Importante para a atividade
O **visual principal da prévia é produzido pelo Particle System clássico**, enquanto o **VFX Graph incluído é um modelo inicial de partículas GPU e precisa ser personalizado com Spawn / Initialize / Update / Output e texturas de fogo**. **Não apresente este projeto como se o fogo todo já fosse implementado no VFX Graph.** É possível apresentar uma prévia visual no Play, mas ainda é necessário transferir o efeito para o grafo para cumprir integralmente o enunciado.

Se o professor forneceu um ZIP de texturas, importe-as e aplique-as no Output Particle Quad do grafo para obter aparência mais realista. Este ZIP não estava disponível nesta conversa.

## Observações
- A cena é gerada pelo Editor; os arquivos `Assets/Scenes/Fogueira.unity`, `Assets/Materials`, `Assets/Settings` e `Assets/VFX` serão criados quando o projeto for aberto no Unity.
- Não foi possível executar a Unity neste ambiente; ajuste eventuais incompatibilidades conforme a versão instalada.
