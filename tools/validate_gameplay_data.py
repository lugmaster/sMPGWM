"""Validate the authored gameplay increment, not the Godot runtime.

Install tools/gameplay-validation-requirements.txt, then run this file from any directory.
"""
from pathlib import Path
import copy
import sys

import yaml
from jsonschema import Draft7Validator


ROOT = Path(__file__).resolve().parents[1] / 'sMPGWM.Game/assets/game_data'
CATALOGUES = {
    'abilities': ('AbilityDefinitions.yml', 'AbilitySchema.yml', 'AbilityDefinitions'),
    'behaviours': ('BehaviourDefinitions.yml', 'BehaviourSchema.yml', 'BehaviourDefinitions'),
    'effects': ('EffectDefinitions.yml', 'EffectDefinitionSchema.yml', 'EffectDefinitions'),
    'targeting': ('TargetingDefinitions.yml', 'TargetingSchema.yml', 'TargetingDefinitions'),
    'projectiles': ('ProjectileDefinitions.yml', 'ProjectileSchema.yml', 'ProjectileDefinitions'),
    'classifications': ('BehaviourClassificationDefinitions.yml', 'BehaviourClassificationSchema.yml', 'BehaviourClassifications'),
    'stats': ('Attributes.yml', 'AttributeSchema.yml', 'Stats'),
    'conditions': ('Conditions.yml', 'ConditionSchema.yml', 'Conditions'),
    'items': ('ItemDefinitions.yml', 'ItemSchema.yml', 'Items'),
}


class UniqueKeyLoader(yaml.SafeLoader):
    pass


def mapping(loader, node, deep=False):
    result = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in result:
            raise ValueError(f'Duplicate YAML key: {key}')
        result[key] = loader.construct_object(value_node, deep=deep)
    return result


UniqueKeyLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, mapping)


def read(name):
    return yaml.load((ROOT / name).read_text(encoding='utf-8'), Loader=UniqueKeyLoader)


def validate(documents, schemas):
    errors = []
    indices = {}
    for kind, (filename, _, key) in CATALOGUES.items():
        document = documents[kind]
        for error in Draft7Validator(schemas[kind]).iter_errors(document):
            errors.append(f'{filename}:{list(error.absolute_path)}: {error.message}')
        indices[kind] = {}
        for entry in document.get(key, []):
            id = entry.get('Id')
            if id in indices[kind]:
                errors.append(f'{filename}: duplicate Id {id}')
            indices[kind][id] = entry
    if errors:
        return errors

    damage_ids = {entry['Id'] for entry in read('DamageSources.yml')['DamageSources']}
    category_ids = {entry['Id'] for entry in read('ItemCategoryDefinitions.yml')['Categories']}
    graph = {}

    def ref(kind, id, owner):
        if id not in indices[kind]:
            errors.append(f'{owner}: unknown {kind} reference {id}')

    def refs(kind, ids, owner):
        for id in ids:
            ref(kind, id, owner)

    def modifier(value, owner):
        ref('stats', value['StatId'], owner)
        pools = {'health', 'shield', 'energy_storage', 'oxygen', 'food', 'water'}
        if value['ValueKind'] == 'Maximum' and value['StatId'] not in pools:
            errors.append(f'{owner}: Maximum requires a resource pool')

    def edge(owner, kind, id):
        ref(kind, id, owner)
        graph.setdefault(owner, []).append(f'{kind}:{id}')

    for id, ability in indices['abilities'].items():
        owner = f'abilities:{id}'
        ref('targeting', ability['TargetingId'], owner)
        ref('effects', ability['EffectId'], owner)
        refs('conditions', ability['BlockedByConditions'], owner)
        if 'Radius' in indices['targeting'].get(ability['TargetingId'], {}):
            errors.append(f'{owner}: selected-target abilities cannot use an area profile')
        for cost in ability['Costs']:
            ref('stats', cost['StatId'], owner)

    for id, effect in indices['effects'].items():
        owner = f'effects:{id}'
        kind = effect['Kind']
        refs('conditions', effect.get('BlockedByConditions', []), owner)
        if kind == 'ApplyBehaviour':
            edge(owner, 'behaviours', effect['BehaviourId'])
        elif kind == 'Sequence':
            for child in effect['EffectIds']:
                edge(owner, 'effects', child)
        elif kind == 'SearchArea':
            ref('targeting', effect['TargetingId'], owner)
            if 'Radius' not in indices['targeting'].get(effect['TargetingId'], {}):
                errors.append(f'{owner}: area search requires a radius profile')
            edge(owner, 'effects', effect['EffectId'])
        elif kind == 'LaunchProjectile':
            ref('projectiles', effect['ProjectileId'], owner)
            edge(owner, 'effects', effect['ImpactEffectId'])
        elif kind == 'RestoreResource':
            ref('stats', effect['StatId'], owner)
            if effect['StatId'] not in {'health', 'shield', 'energy_storage', 'oxygen', 'food', 'water'}:
                errors.append(f'{owner}: resource restoration requires a pool')
        elif kind == 'DealDamage' and effect['DamageSourceId'] not in damage_ids:
            errors.append(f'{owner}: unknown damage source')
        elif kind == 'RemoveBehaviours':
            refs('classifications', effect['Selector'].get('ClassificationIds', []), owner)
            refs('stats', effect['Selector'].get('ModifiedStatIds', []), owner)

    for id, behaviour in indices['behaviours'].items():
        owner = f'behaviours:{id}'
        refs('classifications', behaviour.get('ClassificationIds', []), owner)
        for key in ('GrantedConditions', 'GrantedConditionsAfterFirstTick', 'BlockedByConditions'):
            refs('conditions', behaviour.get(key, []), owner)
        for value in behaviour.get('Modifiers', []):
            modifier(value, owner)
        if 'PeriodicAccumulator' in behaviour:
            periodic = behaviour['PeriodicAccumulator']
            modifier(periodic['Modifier'], owner)
            refs('conditions', periodic['BlockedByConditions'], owner)
            if behaviour['Lifetime']['Kind'] == 'Timed' and periodic['IntervalMs'] >= behaviour['Lifetime']['DurationMs']:
                errors.append(f'{owner}: no periodic tick can run before expiration')
        for key in ('OnApplyEffectId', 'OnExpireEffectId'):
            if key in behaviour:
                edge(owner, 'effects', behaviour[key])
        if 'OnRemove' in behaviour:
            edge(owner, 'effects', behaviour['OnRemove']['EffectId'])
        if behaviour.get('OnExpireEffectId') and behaviour['Lifetime']['Kind'] != 'Timed':
            errors.append(f'{owner}: expiration hook requires a timed lifetime')

    for id, item in indices['items'].items():
        owner = f'items:{id}'
        if item['CategoryId'] not in category_ids:
            errors.append(f'{owner}: unknown item category')
        refs('abilities', item.get('AbilityIds', []), owner)
        for grant in item.get('BehaviourGrants', []):
            ref('behaviours', grant['BehaviourId'], owner)

    visiting, visited = set(), set()

    def visit(node):
        if node in visiting:
            errors.append(f'Effect/behaviour cycle at {node}')
            return
        if node in visited:
            return
        visiting.add(node)
        for child in graph.get(node, []):
            visit(child)
        visiting.remove(node)
        visited.add(node)

    for node in graph:
        visit(node)
    return errors


def negative_checks(documents, schemas):
    cases = [
        ('unknown field', lambda d: d['abilities']['AbilityDefinitions'][0].update(Typo=True)),
        ('dangling reference', lambda d: d['abilities']['AbilityDefinitions'][0].update(EffectId='missing')),
        ('duplicate ID', lambda d: d['effects']['EffectDefinitions'].append(copy.deepcopy(d['effects']['EffectDefinitions'][0]))),
        ('wrong operation payload', lambda d: d['effects']['EffectDefinitions'][0].update(Kind='DealDamage')),
        ('invalid percentage', lambda d: d['effects']['EffectDefinitions'][1]['Amount'].update(Value=10)),
        ('wrong stat kind', lambda d: d['behaviours']['BehaviourDefinitions'][1]['Modifiers'][0].update(ValueKind='Maximum')),
        ('lifecycle cycle', lambda d: d['behaviours']['BehaviourDefinitions'][1].update(OnExpireEffectId='infection')),
        ('wrong area profile', lambda d: next(e for e in d['effects']['EffectDefinitions'] if e['Kind']=='SearchArea').update(TargetingId='self')),
    ]
    for name, mutate in cases:
        bad = copy.deepcopy(documents)
        mutate(bad)
        if not validate(bad, schemas):
            raise AssertionError(f'Validator accepted {name}')
    print(f'{len(cases)} invalid-content checks rejected as expected.')


def main():
    documents = {kind: read(paths[0]) for kind, paths in CATALOGUES.items()}
    schemas = {kind: read(paths[1]) for kind, paths in CATALOGUES.items()}
    for schema in schemas.values():
        Draft7Validator.check_schema(schema)
    errors = validate(documents, schemas)
    if errors:
        print('\n'.join(errors))
        return 1
    count = sum(len(documents[k][v[2]]) for k,v in CATALOGUES.items())
    print(f'Validated {count} definitions across {len(CATALOGUES)} catalogues, references, and graph cycles.')
    negative_checks(documents, schemas)
    return 0


if __name__ == '__main__':
    sys.exit(main())
