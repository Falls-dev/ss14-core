using Content.Server._White.Genetics.Components;

namespace Content.Server._White.Genetics.Systems;

public sealed partial class GenomeSystem
{
    public void CheckInstability(EntityUid uid, GenomeComponent comp, int delta)
    {
        if (delta > 0)
        {
            if (comp.Instability < 100)
                return;

            // если эффекты уже готовятся примениться, но сущность себе еще мутации колит - надо отменить подготовку, пересчитать и применить новые

            // TODO: нужна прикольная функция плотности вероятности p(instability, x), где х - "плохость" эффекта
            // ну или другой способ задать распределение для произвольного количества эффектов с произвольными показателями плохости и для произвольной 200 (?) > Instability > 100
        }



    }
}
